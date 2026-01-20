using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;
using PayRollManagementSystem.Services;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly WorkingDaysService _workingDaysService;
        private readonly HolidayService _holidayService;

        public AttendanceController(ApplicationDbContext context, WorkingDaysService workingDaysService, HolidayService holidayService)
        {
            _context = context;
            _workingDaysService = workingDaysService;
            _holidayService = holidayService;
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
            
            var today = DateTime.Today;
            ViewBag.TodayDate = today.ToString("yyyy-MM-dd");
            
            // Check if today is weekend or holiday
            var isWorkingDay = await _workingDaysService.IsWorkingDay(today);
            var holiday = await _holidayService.GetHolidayByDate(today);
            var weekendDays = await _workingDaysService.GetConfiguredWeekendDays();
            
            ViewBag.IsWorkingDay = isWorkingDay;
            ViewBag.Holiday = holiday;
            ViewBag.IsWeekend = weekendDays.Contains(today.DayOfWeek);
            
            // IMPORTANT: Ensure these are never null
            if (ViewBag.IsWorkingDay == null) ViewBag.IsWorkingDay = true;
            if (ViewBag.IsWeekend == null) ViewBag.IsWeekend = false;
            
            return View();
        }

        // POST: Attendance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,Date,CheckInTime,CheckOutTime,Status,Remarks")] Attendance attendance)
        {
            // Check if the date is a weekend or holiday
            var isWorkingDay = await _workingDaysService.IsWorkingDay(attendance.Date);
            if (!isWorkingDay)
            {
                var holiday = await _holidayService.GetHolidayByDate(attendance.Date);
                if (holiday != null)
                {
                    ModelState.AddModelError("Date", $"Cannot mark attendance on holiday: {holiday.Name}");
                }
                else
                {
                    ModelState.AddModelError("Date", "Cannot mark attendance on weekends");
                }
                
                await LoadDropdownData();
                ViewBag.TodayDate = attendance.Date.ToString("yyyy-MM-dd");
                return View(attendance);
            }

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

                    // Calculate if late arrival
                    var gracePeriodEnd = shift.StartTime.Add(TimeSpan.FromMinutes(shift.GracePeriod));
                    if (attendance.CheckInTime > gracePeriodEnd)
                    {
                        attendance.IsLate = true;
                        attendance.LateByMinutes = (int)(attendance.CheckInTime - shift.StartTime).TotalMinutes;
                        
                        // Update status to Late if currently Present
                        if (attendance.Status == AttendanceStatus.Present)
                        {
                            attendance.Status = AttendanceStatus.Late;
                        }
                    }

                    // Calculate total hours and early leave if checkout time is provided
                    if (attendance.CheckOutTime.HasValue)
                    {
                        var totalMinutes = (attendance.CheckOutTime.Value - attendance.CheckInTime).TotalMinutes;

                        // Handle night shift (checkout time is before check-in time next day)
                        if (shift.IsNightShift && attendance.CheckOutTime.Value < attendance.CheckInTime)
                        {
                            totalMinutes = (new TimeSpan(24, 0, 0) - attendance.CheckInTime + attendance.CheckOutTime.Value).TotalMinutes;
                        }

                        // Subtract break duration
                        totalMinutes -= shift.BreakDuration;
                        attendance.TotalHours = (decimal)(totalMinutes / 60);

                        // Check for early leave (left before shift end time)
                        if (attendance.CheckOutTime.Value < shift.EndTime)
                        {
                            var earlyLeaveMinutes = (int)(shift.EndTime - attendance.CheckOutTime.Value).TotalMinutes;
                            
                            // Only mark as early leave if significant (more than 5 minutes)
                            if (earlyLeaveMinutes > 5)
                            {
                                attendance.IsEarlyLeave = true;
                                attendance.EarlyLeaveByMinutes = earlyLeaveMinutes;
                            }
                        }

                        // Check if half day based on hours worked
                        if (attendance.TotalHours < shift.HalfDayHours)
                        {
                            attendance.IsHalfDay = true;
                            attendance.Status = AttendanceStatus.HalfDay;
                        }

                        // Calculate overtime (only if full day hours are completed)
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
                isEarlyLeave = attendance.IsEarlyLeave,
                earlyLeaveByMinutes = attendance.EarlyLeaveByMinutes,
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

                        // Recalculate total hours and early leave
                        if (attendance.CheckOutTime.HasValue)
                        {
                            var totalMinutes = (attendance.CheckOutTime.Value - attendance.CheckInTime).TotalMinutes;

                            if (shift.IsNightShift && attendance.CheckOutTime.Value < attendance.CheckInTime)
                            {
                                totalMinutes = (new TimeSpan(24, 0, 0) - attendance.CheckInTime + attendance.CheckOutTime.Value).TotalMinutes;
                            }

                            totalMinutes -= shift.BreakDuration;
                            attendance.TotalHours = (decimal)(totalMinutes / 60);

                            // Check for early leave
                            if (attendance.CheckOutTime.Value < shift.EndTime)
                            {
                                var earlyLeaveMinutes = (int)(shift.EndTime - attendance.CheckOutTime.Value).TotalMinutes;
                                
                                if (earlyLeaveMinutes > 5)
                                {
                                    attendance.IsEarlyLeave = true;
                                    attendance.EarlyLeaveByMinutes = earlyLeaveMinutes;
                                }
                                else
                                {
                                    attendance.IsEarlyLeave = false;
                                    attendance.EarlyLeaveByMinutes = null;
                                }
                            }
                            else
                            {
                                attendance.IsEarlyLeave = false;
                                attendance.EarlyLeaveByMinutes = null;
                            }

                            // Check if half day
                            attendance.IsHalfDay = attendance.TotalHours < shift.HalfDayHours;

                            // Calculate overtime
                            attendance.OvertimeHours = attendance.TotalHours > shift.FullDayHours
                                ? attendance.TotalHours - shift.FullDayHours
                                : null;
                        }
                        else
                        {
                            attendance.IsEarlyLeave = false;
                            attendance.EarlyLeaveByMinutes = null;
                        }
                    }

                    existingAttendance.CheckInTime = attendance.CheckInTime;
                    existingAttendance.CheckOutTime = attendance.CheckOutTime;
                    existingAttendance.Status = attendance.Status;
                    existingAttendance.IsLate = attendance.IsLate;
                    existingAttendance.LateByMinutes = attendance.LateByMinutes;
                    existingAttendance.IsEarlyLeave = attendance.IsEarlyLeave;
                    existingAttendance.EarlyLeaveByMinutes = attendance.EarlyLeaveByMinutes;
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
            
            // Check if today is weekend or holiday
            var isWorkingDay = await _workingDaysService.IsWorkingDay(today);
            var holiday = await _holidayService.GetHolidayByDate(today);
            var weekendDays = await _workingDaysService.GetConfiguredWeekendDays();
            
            ViewBag.IsWorkingDay = isWorkingDay;
            ViewBag.Holiday = holiday;
            ViewBag.IsWeekend = weekendDays.Contains(today.DayOfWeek);
            ViewBag.TodayDate = today.ToString("yyyy-MM-dd");
            
            // If not a working day, show message and return empty list
            if (!isWorkingDay)
            {
                if (holiday != null)
                {
                    TempData["Info"] = $"Today is {holiday.Name}. No attendance marking required.";
                }
                else
                {
                    TempData["Info"] = "Today is a weekend. No attendance marking required.";
                }
                return View(new List<Employee>());
            }
            
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

            // Get company settings
            var companySetting = await _context.CompanySettings.FirstOrDefaultAsync();
            ViewBag.CompanySetting = companySetting;

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
                .Include(e => e.ShiftNavigation)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                return NotFound();
            }

            // Use WorkingDaysService for accurate calculation
            var summary = await _workingDaysService.CalculateAttendanceSummary(
                employeeId.Value, 
                currentYear, 
                currentMonth
            );

            // Add employee information to the summary
            ViewBag.Employee = employee;
            ViewBag.MonthName = new DateTime(currentYear, currentMonth, 1).ToString("MMMM yyyy");

            return View(summary);
        }

        // GET: Get employee shift information for real-time calculation (AJAX)
        [HttpGet]
        [Route("/api/GetEmployeeShift/{employeeId}")]
        public async Task<IActionResult> GetEmployeeShift(int employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.ShiftNavigation)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee?.ShiftNavigation == null)
            {
                return Json(new { hasShift = false });
            }

            var shift = employee.ShiftNavigation;
            return Json(new
            {
                hasShift = true,
                shift = new
                {
                    shiftName = shift.ShiftName,
                    startTime = shift.StartTime.ToString(@"hh\:mm"),
                    endTime = shift.EndTime.ToString(@"hh\:mm"),
                    gracePeriod = shift.GracePeriod,
                    breakDuration = shift.BreakDuration,
                    fullDayHours = shift.FullDayHours,
                    halfDayHours = shift.HalfDayHours,
                    isNightShift = shift.IsNightShift
                }
            });
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