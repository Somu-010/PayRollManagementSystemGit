using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Attendance
        public async Task<IActionResult> Index(string searchString, DateTime? fromDate, DateTime? toDate, string status)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentStatus"] = status;

            var attendances = from a in _context.Attendances
                              .Include(a => a.Employee)
                              .ThenInclude(e => e.DepartmentNavigation)
                              .Include(a => a.Employee)
                              .ThenInclude(e => e.ShiftNavigation)
                              select a;

            // Search by employee name or code
            if (!string.IsNullOrEmpty(searchString))
            {
                attendances = attendances.Where(a => a.Employee!.Name.Contains(searchString)
                                               || a.Employee!.EmployeeCode.Contains(searchString));
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                attendances = attendances.Where(a => a.Date >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                attendances = attendances.Where(a => a.Date <= toDate.Value);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AttendanceStatus>(status, out var statusEnum))
            {
                attendances = attendances.Where(a => a.Status == statusEnum);
            }

            return View(await attendances.OrderByDescending(a => a.Date).ThenBy(a => a.Employee!.Name).ToListAsync());
        }

        // GET: Attendance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .ThenInclude(e => e.DepartmentNavigation)
                .Include(a => a.Employee)
                .ThenInclude(e => e.ShiftNavigation)
                .FirstOrDefaultAsync(m => m.AttendanceId == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // GET: Attendance/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdownData();
            ViewBag.TodayDate = DateTime.Today.ToString("yyyy-MM-dd");
            return View();
        }

        // POST: Attendance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,Date,CheckInTime,CheckOutTime,Status,IsLate,LateByMinutes,IsHalfDay,OvertimeHours,Remarks")] Attendance attendance)
        {
            ModelState.Remove("Employee");

            if (ModelState.IsValid)
            {
                // Check if attendance already exists for this employee on this date
                if (await _context.Attendances.AnyAsync(a => a.EmployeeId == attendance.EmployeeId && a.Date == attendance.Date))
                {
                    ModelState.AddModelError("Date", "Attendance already marked for this employee on this date.");
                    await LoadDropdownData();
                    return View(attendance);
                }

                // Get employee's shift information
                var employee = await _context.Employees
                    .Include(e => e.ShiftNavigation)
                    .FirstOrDefaultAsync(e => e.EmployeeId == attendance.EmployeeId);

                if (employee?.ShiftNavigation != null)
                {
                    var shift = employee.ShiftNavigation;

                    // Calculate if late
                    var gracePeriodEnd = shift.StartTime.Add(TimeSpan.FromMinutes(shift.GracePeriod));
                    if (attendance.CheckInTime > gracePeriodEnd)
                    {
                        attendance.IsLate = true;
                        attendance.LateByMinutes = (int)(attendance.CheckInTime - shift.StartTime).TotalMinutes;
                    }

                    // Calculate total hours if checkout time is provided
                    if (attendance.CheckOutTime.HasValue)
                    {
                        var totalMinutes = (attendance.CheckOutTime.Value - attendance.CheckInTime).TotalMinutes;

                        // Handle night shift
                        if (shift.IsNightShift && attendance.CheckOutTime.Value < attendance.CheckInTime)
                        {
                            totalMinutes = (new TimeSpan(24, 0, 0) - attendance.CheckInTime + attendance.CheckOutTime.Value).TotalMinutes;
                        }

                        // Subtract break duration
                        totalMinutes -= shift.BreakDuration;
                        attendance.TotalHours = (decimal)(totalMinutes / 60);

                        // Check if half day
                        if (attendance.TotalHours < shift.HalfDayHours)
                        {
                            attendance.IsHalfDay = true;
                            attendance.Status = AttendanceStatus.HalfDay;
                        }

                        // Calculate overtime
                        if (attendance.TotalHours > shift.FullDayHours)
                        {
                            attendance.OvertimeHours = attendance.TotalHours - shift.FullDayHours;
                        }
                    }
                }

                attendance.CreatedAt = DateTime.Now;
                _context.Add(attendance);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Attendance marked successfully!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownData();
            return View(attendance);
        }

        // GET: Get attendance data for editing (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetAttendance(int id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.AttendanceId == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return Json(new
            {
                attendanceId = attendance.AttendanceId,
                employeeId = attendance.EmployeeId,
                employeeName = attendance.Employee?.Name,
                date = attendance.Date.ToString("yyyy-MM-dd"),
                checkInTime = attendance.CheckInTime.ToString(@"hh\:mm"),
                checkOutTime = attendance.CheckOutTime?.ToString(@"hh\:mm"),
                status = attendance.Status.ToString(),
                isLate = attendance.IsLate,
                lateByMinutes = attendance.LateByMinutes,
                isHalfDay = attendance.IsHalfDay,
                overtimeHours = attendance.OvertimeHours,
                remarks = attendance.Remarks
            });
        }

        // POST: Attendance/Edit (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] Attendance attendance)
        {
            ModelState.Remove("Employee");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAttendance = await _context.Attendances.FindAsync(attendance.AttendanceId);
                    if (existingAttendance == null)
                    {
                        return Json(new { success = false, message = "Attendance record not found." });
                    }

                    // Get employee's shift for recalculation
                    var employee = await _context.Employees
                        .Include(e => e.ShiftNavigation)
                        .FirstOrDefaultAsync(e => e.EmployeeId == attendance.EmployeeId);

                    if (employee?.ShiftNavigation != null)
                    {
                        var shift = employee.ShiftNavigation;

                        // Recalculate late status
                        var gracePeriodEnd = shift.StartTime.Add(TimeSpan.FromMinutes(shift.GracePeriod));
                        if (attendance.CheckInTime > gracePeriodEnd)
                        {
                            attendance.IsLate = true;
                            attendance.LateByMinutes = (int)(attendance.CheckInTime - shift.StartTime).TotalMinutes;
                        }
                        else
                        {
                            attendance.IsLate = false;
                            attendance.LateByMinutes = null;
                        }

                        // Recalculate total hours
                        if (attendance.CheckOutTime.HasValue)
                        {
                            var totalMinutes = (attendance.CheckOutTime.Value - attendance.CheckInTime).TotalMinutes;

                            if (shift.IsNightShift && attendance.CheckOutTime.Value < attendance.CheckInTime)
                            {
                                totalMinutes = (new TimeSpan(24, 0, 0) - attendance.CheckInTime + attendance.CheckOutTime.Value).TotalMinutes;
                            }

                            totalMinutes -= shift.BreakDuration;
                            attendance.TotalHours = (decimal)(totalMinutes / 60);

                            // Check if half day
                            attendance.IsHalfDay = attendance.TotalHours < shift.HalfDayHours;

                            // Calculate overtime
                            attendance.OvertimeHours = attendance.TotalHours > shift.FullDayHours
                                ? attendance.TotalHours - shift.FullDayHours
                                : null;
                        }
                    }

                    existingAttendance.CheckInTime = attendance.CheckInTime;
                    existingAttendance.CheckOutTime = attendance.CheckOutTime;
                    existingAttendance.Status = attendance.Status;
                    existingAttendance.IsLate = attendance.IsLate;
                    existingAttendance.LateByMinutes = attendance.LateByMinutes;
                    existingAttendance.IsHalfDay = attendance.IsHalfDay;
                    existingAttendance.TotalHours = attendance.TotalHours;
                    existingAttendance.OvertimeHours = attendance.OvertimeHours;
                    existingAttendance.Remarks = attendance.Remarks;
                    existingAttendance.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Attendance updated successfully!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AttendanceExists(attendance.AttendanceId))
                    {
                        return Json(new { success = false, message = "Attendance record not found." });
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        // POST: Attendance/Delete (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.AttendanceId == id);

            if (attendance == null)
            {
                return Json(new { success = false, message = "Attendance record not found." });
            }

            var employeeName = attendance.Employee?.Name ?? "Employee";
            var date = attendance.Date.ToString("yyyy-MM-dd");

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Attendance for {employeeName} on {date} deleted successfully!" });
        }

        // Bulk Mark Attendance for Today
        [HttpGet]
        public async Task<IActionResult> BulkMarkAttendance()
        {
            var today = DateTime.Today;
            var employees = await _context.Employees
                .Include(e => e.ShiftNavigation)
                .Where(e => e.Status == EmploymentStatus.Active)
                .ToListAsync();

            // Check which employees already have attendance marked
            var markedEmployeeIds = await _context.Attendances
                .Where(a => a.Date == today)
                .Select(a => a.EmployeeId)
                .ToListAsync();

            var unmarkedEmployees = employees.Where(e => !markedEmployeeIds.Contains(e.EmployeeId)).ToList();

            ViewBag.TodayDate = today.ToString("yyyy-MM-dd");
            ViewBag.MarkedCount = markedEmployeeIds.Count;
            ViewBag.UnmarkedCount = unmarkedEmployees.Count;

            return View(unmarkedEmployees);
        }

        // POST: Bulk Mark Attendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkMarkAttendance(List<int> selectedEmployees, string bulkStatus)
        {
            if (selectedEmployees == null || !selectedEmployees.Any())
            {
                TempData["Error"] = "Please select at least one employee.";
                return RedirectToAction(nameof(BulkMarkAttendance));
            }

            var today = DateTime.Today;
            var currentTime = DateTime.Now.TimeOfDay;

            if (!Enum.TryParse<AttendanceStatus>(bulkStatus, out var status))
            {
                status = AttendanceStatus.Present;
            }

            foreach (var employeeId in selectedEmployees)
            {
                // Check if already marked
                if (await _context.Attendances.AnyAsync(a => a.EmployeeId == employeeId && a.Date == today))
                {
                    continue;
                }

                var employee = await _context.Employees
                    .Include(e => e.ShiftNavigation)
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null) continue;

                var attendance = new Attendance
                {
                    EmployeeId = employeeId,
                    Date = today,
                    CheckInTime = employee.ShiftNavigation?.StartTime ?? currentTime,
                    Status = status,
                    CreatedAt = DateTime.Now
                };

                // Calculate late status if Present
                if (status == AttendanceStatus.Present && employee.ShiftNavigation != null)
                {
                    var shift = employee.ShiftNavigation;
                    var gracePeriodEnd = shift.StartTime.Add(TimeSpan.FromMinutes(shift.GracePeriod));

                    if (currentTime > gracePeriodEnd)
                    {
                        attendance.IsLate = true;
                        attendance.LateByMinutes = (int)(currentTime - shift.StartTime).TotalMinutes;
                        attendance.Status = AttendanceStatus.Late;
                    }
                }

                _context.Attendances.Add(attendance);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Attendance marked for {selectedEmployees.Count} employee(s) successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Monthly Attendance Report
        [HttpGet]
        public async Task<IActionResult> MonthlyReport(int? employeeId, int? month, int? year)
        {
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            ViewBag.SelectedMonth = currentMonth;
            ViewBag.SelectedYear = currentYear;
            ViewBag.SelectedEmployeeId = employeeId;

            // Load employees for dropdown
            ViewBag.Employees = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.Name)
                .Select(e => new { e.EmployeeId, e.Name, e.EmployeeCode })
                .ToListAsync();

            if (!employeeId.HasValue)
            {
                return View(null);
            }

            var employee = await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                return NotFound();
            }

            var startDate = new DateTime(currentYear, currentMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
                .OrderBy(a => a.Date)
                .ToListAsync();

            // Calculate summary
            var summary = new
            {
                Employee = employee,
                Month = startDate.ToString("MMMM yyyy"),
                TotalDays = (endDate - startDate).Days + 1,
                PresentDays = attendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late),
                AbsentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent),
                LateDays = attendances.Count(a => a.IsLate),
                HalfDays = attendances.Count(a => a.IsHalfDay),
                LeaveDays = attendances.Count(a => a.Status == AttendanceStatus.OnLeave),
                TotalWorkingHours = attendances.Sum(a => a.TotalHours ?? 0),
                TotalOvertimeHours = attendances.Sum(a => a.OvertimeHours ?? 0),
                Attendances = attendances
            };

            return View(summary);
        }

        private bool AttendanceExists(int id)
        {
            return _context.Attendances.Any(e => e.AttendanceId == id);
        }

        private async Task LoadDropdownData()
        {
            ViewBag.Employees = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.Name)
                .Select(e => new { e.EmployeeId, e.Name, e.EmployeeCode })
                .ToListAsync();
        }
    }
}