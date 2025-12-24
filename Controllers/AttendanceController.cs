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
        public async Task<IActionResult> Index(DateTime? date, int? employeeId, string status)
        {
            ViewData["CurrentDate"] = date?.ToString("yyyy-MM-dd");
            ViewData["CurrentEmployee"] = employeeId;
            ViewData["CurrentStatus"] = status;

            var attendances = from a in _context.Attendances
                              .Include(a => a.Employee)
                              select a;

            // Filter by date
            if (date.HasValue)
            {
                attendances = attendances.Where(a => a.Date.Date == date.Value.Date);
            }
            else
            {
                // Default: Show today's attendance
                attendances = attendances.Where(a => a.Date.Date == DateTime.Today);
            }

            // Filter by employee
            if (employeeId.HasValue)
            {
                attendances = attendances.Where(a => a.EmployeeId == employeeId.Value);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AttendanceStatus>(status, out var statusEnum))
            {
                attendances = attendances.Where(a => a.Status == statusEnum);
            }

            // Load employees for filter dropdown
            ViewBag.Employees = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.Name)
                .Select(e => new { e.EmployeeId, e.Name, e.EmployeeCode })
                .ToListAsync();

            return View(await attendances.OrderBy(a => a.Employee.Name).ToListAsync());
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
                    .ThenInclude(e => e.ShiftNavigation)
                .Include(a => a.Employee.DepartmentNavigation)
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
            ViewBag.Employees = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.Name)
                .Select(e => new { e.EmployeeId, e.Name, e.EmployeeCode })
                .ToListAsync();

            ViewBag.DefaultDate = DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.DefaultCheckInTime = DateTime.Now.ToString("HH:mm");

            return View();
        }

        // POST: Attendance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,Date,CheckInTime,CheckOutTime,Status,Remarks")] Attendance attendance)
        {
            if (ModelState.IsValid)
            {
                // Check if attendance already exists
                var exists = await _context.Attendances
                    .AnyAsync(a => a.EmployeeId == attendance.EmployeeId && a.Date.Date == attendance.Date.Date);

                if (exists)
                {
                    ModelState.AddModelError("Date", "Attendance already marked for this employee on this date.");
                    await LoadEmployeesDropdown();
                    ViewBag.DefaultDate = attendance.Date.ToString("yyyy-MM-dd");
                    return View(attendance);
                }

                // Calculate attendance metrics
                await CalculateAttendanceMetrics(attendance);

                attendance.CreatedAt = DateTime.Now;
                _context.Add(attendance);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Attendance marked successfully for {attendance.Date:dd MMM yyyy}!";
                return RedirectToAction(nameof(Index));
            }

            await LoadEmployeesDropdown();
            ViewBag.DefaultDate = attendance.Date.ToString("yyyy-MM-dd");
            return View(attendance);
        }

        // AJAX: Get Attendance for Edit Modal
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
                checkInTime = $"{attendance.CheckInTime.Hours:D2}:{attendance.CheckInTime.Minutes:D2}",
                checkOutTime = attendance.CheckOutTime.HasValue
                    ? $"{attendance.CheckOutTime.Value.Hours:D2}:{attendance.CheckOutTime.Value.Minutes:D2}"
                    : "",
                status = attendance.Status.ToString(),
                remarks = attendance.Remarks
            });
        }

        // POST: AJAX Edit Attendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Attendance attendance)
        {
            try
            {
                var existingAttendance = await _context.Attendances.FindAsync(attendance.AttendanceId);
                if (existingAttendance == null)
                {
                    return Json(new { success = false, message = "Attendance record not found" });
                }

                existingAttendance.CheckInTime = attendance.CheckInTime;
                existingAttendance.CheckOutTime = attendance.CheckOutTime;
                existingAttendance.Status = attendance.Status;
                existingAttendance.Remarks = attendance.Remarks;
                existingAttendance.UpdatedAt = DateTime.Now;

                // Recalculate metrics
                await CalculateAttendanceMetrics(existingAttendance);

                _context.Update(existingAttendance);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Attendance updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating attendance: " + ex.Message });
            }
        }

        // POST: AJAX Delete Attendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.Employee)
                    .FirstOrDefaultAsync(a => a.AttendanceId == id);

                if (attendance == null)
                {
                    return Json(new { success = false, message = "Attendance record not found" });
                }

                var employeeName = attendance.Employee?.Name;
                var date = attendance.Date.ToString("dd MMM yyyy");

                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Attendance for {employeeName} on {date} deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting attendance: " + ex.Message });
            }
        }

        // Helper: Calculate attendance metrics
        private async Task CalculateAttendanceMetrics(Attendance attendance)
        {
            var employee = await _context.Employees
                .Include(e => e.ShiftNavigation)
                .FirstOrDefaultAsync(e => e.EmployeeId == attendance.EmployeeId);

            if (employee?.ShiftNavigation == null)
                return;

            var shift = employee.ShiftNavigation;

            // Check if late (only for Present status)
            if (attendance.Status == AttendanceStatus.Present)
            {
                var shiftStartTime = shift.StartTime;
                var lateMarkAfter = TimeSpan.FromMinutes(shift.LateMarkAfter);

                if (attendance.CheckInTime > shiftStartTime.Add(lateMarkAfter))
                {
                    attendance.IsLate = true;
                    attendance.LateByMinutes = (int)(attendance.CheckInTime - shiftStartTime).TotalMinutes;
                    attendance.Status = AttendanceStatus.Late;
                }
            }

            // Calculate total hours if checkout is present
            if (attendance.CheckOutTime.HasValue)
            {
                var workDuration = attendance.CheckOutTime.Value - attendance.CheckInTime;

                // Handle negative duration (checkout next day)
                if (workDuration.TotalMinutes < 0)
                {
                    workDuration = workDuration.Add(TimeSpan.FromHours(24));
                }

                attendance.TotalHours = (decimal)workDuration.TotalHours;

                // Check if half day
                if (attendance.TotalHours < shift.HalfDayHours && attendance.Status == AttendanceStatus.Present)
                {
                    attendance.IsHalfDay = true;
                    attendance.Status = AttendanceStatus.HalfDay;
                }

                // Calculate overtime
                var expectedHours = shift.FullDayHours - ((decimal)shift.BreakDuration / 60m);
                if (attendance.TotalHours > expectedHours)
                {
                    attendance.OvertimeHours = attendance.TotalHours - expectedHours;
                }
            }
        }

        private async Task LoadEmployeesDropdown()
        {
            ViewBag.Employees = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.Name)
                .Select(e => new { e.EmployeeId, e.Name, e.EmployeeCode })
                .ToListAsync();
        }
    }
}
