using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;
using PayRollManagementSystem.Services;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]  // Allow any authenticated user initially
    public class EmployeePortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly HolidayService _holidayService;
        private readonly WorkingDaysService _workingDaysService;

        public EmployeePortalController(
            ApplicationDbContext context, 
            UserManager<IdentityUser> userManager,
            IEmailService emailService,
            HolidayService holidayService,
            WorkingDaysService workingDaysService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _holidayService = holidayService;
            _workingDaysService = workingDaysService;
        }

        // GET: EmployeePortal - Dashboard
        public async Task<IActionResult> Index()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            // Ensure user has Employee role
            var user = await _userManager.GetUserAsync(User);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Employee"))
            {
                await _userManager.AddToRoleAsync(user, "Employee");
            }

            // Get current month stats
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Attendance stats for current month
            var monthlyAttendance = await _context.Attendances
                .Where(a => a.EmployeeId == employee.EmployeeId && 
                           a.Date >= firstDayOfMonth && a.Date <= lastDayOfMonth)
                .ToListAsync();

            ViewBag.PresentDays = monthlyAttendance.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
            ViewBag.AbsentDays = monthlyAttendance.Count(a => a.Status == AttendanceStatus.Absent);
            ViewBag.LateDays = monthlyAttendance.Count(a => a.IsLate);
            ViewBag.LeaveDays = monthlyAttendance.Count(a => a.Status == AttendanceStatus.OnLeave);
            ViewBag.TotalWorkingHours = monthlyAttendance.Sum(a => a.TotalHours ?? 0);
            ViewBag.OvertimeHours = monthlyAttendance.Sum(a => a.OvertimeHours ?? 0);

            // Leave balance
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employee.EmployeeId && lb.Year == today.Year);
            ViewBag.LeaveBalance = leaveBalance;

            // Pending leave requests
            var pendingLeaves = await _context.Leaves
                .Where(l => l.EmployeeId == employee.EmployeeId && l.Status == LeaveStatus.Pending)
                .CountAsync();
            ViewBag.PendingLeaves = pendingLeaves;

            // Latest payslip
            var latestPayroll = await _context.Payrolls
                .Where(p => p.EmployeeId == employee.EmployeeId && p.Status == PayrollStatus.Paid)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .FirstOrDefaultAsync();
            ViewBag.LatestPayroll = latestPayroll;

            // Today's attendance
            var todayAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == today);
            ViewBag.TodayAttendance = todayAttendance;

            // Upcoming holidays
            var upcomingHolidays = await _holidayService.GetUpcomingHolidays(5);
            ViewBag.UpcomingHolidays = upcomingHolidays;

            // Check if today is a holiday
            var todayHoliday = await _holidayService.GetHolidayByDate(today);
            ViewBag.TodayHoliday = todayHoliday;

            return View(employee);
        }

        // GET: EmployeePortal/Profile
        public async Task<IActionResult> Profile()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            return View(employee);
        }

        // GET: EmployeePortal/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            return View(employee);
        }

        // POST: EmployeePortal/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile([Bind("EmployeeId,Phone,Address,City")] Employee model)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            // Only allow updating specific fields
            employee.Phone = model.Phone;
            employee.Address = model.Address;
            employee.City = model.City;
            employee.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                
                // Send email notification
                try
                {
                    await _emailService.SendProfileUpdateNotificationAsync(employee.Email, employee.Name);
                }
                catch (Exception ex)
                {
                    // Log email error but don't fail the profile update
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                }
                
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to update profile. Please try again.";
                return View(employee);
            }
        }

        // GET: EmployeePortal/Attendance
        public async Task<IActionResult> Attendance(int? month, int? year)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            var selectedMonth = month ?? DateTime.Now.Month;
            var selectedYear = year ?? DateTime.Now.Year;

            ViewData["SelectedMonth"] = selectedMonth;
            ViewData["SelectedYear"] = selectedYear;

            var firstDayOfMonth = new DateTime(selectedYear, selectedMonth, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var attendanceList = await _context.Attendances
                .Where(a => a.EmployeeId == employee.EmployeeId &&
                           a.Date >= firstDayOfMonth && a.Date <= lastDayOfMonth)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            // Calculate working days (excluding weekends and holidays)
            var totalWorkingDays = await _workingDaysService.GetWorkingDaysInMonth(selectedYear, selectedMonth);
            var totalCalendarDays = _workingDaysService.GetTotalDaysInMonth(selectedYear, selectedMonth);
            
            // Calculate stats
            ViewBag.TotalDays = totalWorkingDays;  // Show working days, not calendar days
            ViewBag.TotalCalendarDays = totalCalendarDays;
            ViewBag.PresentDays = attendanceList.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
            ViewBag.AbsentDays = attendanceList.Count(a => a.Status == AttendanceStatus.Absent);
            ViewBag.LateDays = attendanceList.Count(a => a.IsLate);
            ViewBag.LeaveDays = attendanceList.Count(a => a.Status == AttendanceStatus.OnLeave);
            ViewBag.TotalHours = attendanceList.Sum(a => a.TotalHours ?? 0);
            ViewBag.OvertimeHours = attendanceList.Sum(a => a.OvertimeHours ?? 0);

            // Get today's attendance status
            var todayAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == DateTime.Today);
            ViewBag.TodayAttendance = todayAttendance;

            // Get holidays for the month
            var holidays = await _context.Holidays
                .Where(h => h.IsActive && h.Date >= firstDayOfMonth && h.Date <= lastDayOfMonth)
                .OrderBy(h => h.Date)
                .ToListAsync();
            ViewBag.Holidays = holidays;

            return View(attendanceList);
        }

        // GET: EmployeePortal/MarkAttendance
        public async Task<IActionResult> MarkAttendance()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            // Check if already marked today
            var todayAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == DateTime.Today);

            ViewBag.TodayAttendance = todayAttendance;
            ViewBag.CurrentTime = DateTime.Now.ToString("HH:mm");
            
            return View(employee);
        }

        // POST: EmployeePortal/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            // Check if already checked in today
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == DateTime.Today);

            if (existingAttendance != null)
            {
                return Json(new { success = false, message = "You have already checked in today!" });
            }

            // Check if today is a holiday
            var todayHoliday = await _context.Holidays
                .FirstOrDefaultAsync(h => h.IsActive && h.Date == DateTime.Today);

            if (todayHoliday != null)
            {
                return Json(new { success = false, message = $"Today is a holiday: {todayHoliday.Name}" });
            }

            var currentTime = DateTime.Now.TimeOfDay;
            var attendance = new Attendance
            {
                EmployeeId = employee.EmployeeId,
                Date = DateTime.Today,
                CheckInTime = currentTime,
                Status = AttendanceStatus.Present,
                CreatedAt = DateTime.Now
            };

            // Calculate late status if shift is defined
            if (employee.ShiftNavigation != null)
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
            await _context.SaveChangesAsync();

            var statusMessage = attendance.IsLate 
                ? $"Checked in at {currentTime:hh\\:mm}. You are {attendance.LateByMinutes} minutes late."
                : $"Checked in successfully at {currentTime:hh\\:mm}!";

            return Json(new { success = true, message = statusMessage, isLate = attendance.IsLate });
        }

        // POST: EmployeePortal/CheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == DateTime.Today);

            if (attendance == null)
            {
                return Json(new { success = false, message = "You haven't checked in yet today!" });
            }

            if (attendance.CheckOutTime.HasValue)
            {
                return Json(new { success = false, message = "You have already checked out today!" });
            }

            var currentTime = DateTime.Now.TimeOfDay;
            attendance.CheckOutTime = currentTime;

            // Calculate total hours and other metrics
            if (employee.ShiftNavigation != null)
            {
                var shift = employee.ShiftNavigation;
                var totalMinutes = (currentTime - attendance.CheckInTime).TotalMinutes;

                // Handle night shift
                if (shift.IsNightShift && currentTime < attendance.CheckInTime)
                {
                    totalMinutes = (new TimeSpan(24, 0, 0) - attendance.CheckInTime + currentTime).TotalMinutes;
                }

                // Subtract break duration
                totalMinutes -= shift.BreakDuration;
                attendance.TotalHours = (decimal)(totalMinutes / 60);

                // Check for early leave
                if (currentTime < shift.EndTime)
                {
                    var earlyLeaveMinutes = (int)(shift.EndTime - currentTime).TotalMinutes;
                    if (earlyLeaveMinutes > 5)
                    {
                        attendance.IsEarlyLeave = true;
                        attendance.EarlyLeaveByMinutes = earlyLeaveMinutes;
                    }
                }

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
            else
            {
                // No shift defined, just calculate basic hours
                var totalMinutes = (currentTime - attendance.CheckInTime).TotalMinutes;
                attendance.TotalHours = (decimal)(totalMinutes / 60);
            }

            attendance.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var message = $"Checked out at {currentTime:hh\\:mm}. Total hours: {attendance.TotalHours:0.00}";
            if (attendance.OvertimeHours > 0)
            {
                message += $". Overtime: {attendance.OvertimeHours:0.00} hours";
            }

            return Json(new { 
                success = true, 
                message = message,
                totalHours = attendance.TotalHours,
                overtimeHours = attendance.OvertimeHours ?? 0,
                isEarlyLeave = attendance.IsEarlyLeave
            });
        }

        // GET: EmployeePortal/Payslips
        public async Task<IActionResult> Payslips(int? year)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            var selectedYear = year ?? DateTime.Now.Year;
            ViewData["SelectedYear"] = selectedYear;

            var payslips = await _context.Payrolls
                .Include(p => p.PayrollDetails)
                    .ThenInclude(pd => pd.AllowanceDeduction)
                .Where(p => p.EmployeeId == employee.EmployeeId && p.Year == selectedYear)
                .OrderByDescending(p => p.Month)
                .ToListAsync();

            // Calculate yearly totals
            ViewBag.TotalEarnings = payslips.Sum(p => p.GrossSalary);
            ViewBag.TotalDeductions = payslips.Sum(p => p.TotalDeductions);
            ViewBag.TotalNetPay = payslips.Sum(p => p.NetSalary);

            return View(payslips);
        }

        // GET: EmployeePortal/PayslipDetails/5
        public async Task<IActionResult> PayslipDetails(int? id)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                    .ThenInclude(e => e!.DepartmentNavigation)
                .Include(p => p.Employee)
                    .ThenInclude(e => e!.DesignationNavigation)
                .Include(p => p.PayrollDetails)
                    .ThenInclude(pd => pd.AllowanceDeduction)
                .FirstOrDefaultAsync(p => p.PayrollId == id && p.EmployeeId == employee.EmployeeId);

            if (payroll == null)
            {
                return NotFound();
            }

            // Get company settings
            var companySetting = await _context.CompanySettings.FirstOrDefaultAsync();
            ViewBag.CompanySetting = companySetting;

            return View(payroll);
        }

        // GET: EmployeePortal/LeaveRequests
        public async Task<IActionResult> LeaveRequests(string? status)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            ViewData["CurrentStatus"] = status;

            var query = _context.Leaves
                .Where(l => l.EmployeeId == employee.EmployeeId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveStatus>(status, out var leaveStatus))
            {
                query = query.Where(l => l.Status == leaveStatus);
            }

            var leaves = await query
                .OrderByDescending(l => l.AppliedOn)
                .ToListAsync();

            // Get leave balance
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employee.EmployeeId && lb.Year == DateTime.Now.Year);
            ViewBag.LeaveBalance = leaveBalance;

            return View(leaves);
        }

        // GET: EmployeePortal/ApplyLeave
        public async Task<IActionResult> ApplyLeave()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            // Get leave balance
            var leaveBalance = await GetOrCreateLeaveBalance(employee.EmployeeId, DateTime.Now.Year);
            ViewBag.LeaveBalance = leaveBalance;
            ViewBag.Employee = employee;

            return View();
        }

        // POST: EmployeePortal/ApplyLeave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyLeave([Bind("LeaveType,StartDate,EndDate,Reason,IsHalfDay")] Leave leave)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            ModelState.Remove("Employee");

            if (ModelState.IsValid)
            {
                // Validate dates
                if (leave.EndDate < leave.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after or equal to start date.");
                    
                    // Reload leave balance for view
                    var leaveBalanceError = await GetOrCreateLeaveBalance(employee.EmployeeId, DateTime.Now.Year);
                    ViewBag.LeaveBalance = leaveBalanceError;
                    return View(leave);
                }

                if (leave.StartDate < DateTime.Today)
                {
                    ModelState.AddModelError("StartDate", "Cannot apply leave for past dates.");
                    
                    // Reload leave balance for view
                    var leaveBalanceError = await GetOrCreateLeaveBalance(employee.EmployeeId, DateTime.Now.Year);
                    ViewBag.LeaveBalance = leaveBalanceError;
                    return View(leave);
                }

                // Calculate number of days
                var requestedDays = leave.IsHalfDay ? 0.5m : (leave.EndDate - leave.StartDate).Days + 1;

                // Validate maternity leave eligibility
                if (leave.LeaveType == LeaveType.MaternityLeave)
                {
                    if (employee.Gender != Gender.Female)
                    {
                        ModelState.AddModelError("LeaveType", "Maternity leave is only available for female employees.");
                        
                        // Reload leave balance for view
                        var leaveBalanceError = await GetOrCreateLeaveBalance(employee.EmployeeId, DateTime.Now.Year);
                        ViewBag.LeaveBalance = leaveBalanceError;
                        return View(leave);
                    }
                }

                // Check leave balance
                var leaveBalance = await GetOrCreateLeaveBalance(employee.EmployeeId, DateTime.Now.Year);
                var hasBalance = CheckLeaveBalance(leaveBalance, leave.LeaveType, requestedDays);

                if (!hasBalance)
                {
                    var remainingDays = GetRemainingLeaveBalance(leaveBalance, leave.LeaveType);
                    ModelState.AddModelError("", $"Insufficient {leave.LeaveType} balance. You have {remainingDays} days remaining but requested {requestedDays} days.");
                    
                    // Reload leave balance for view
                    ViewBag.LeaveBalance = leaveBalance;
                    return View(leave);
                }

                // Check for overlapping leaves
                var existingLeave = await _context.Leaves
                    .Where(l => l.EmployeeId == employee.EmployeeId &&
                               l.Status != LeaveStatus.Rejected &&
                               l.Status != LeaveStatus.Cancelled &&
                               ((leave.StartDate >= l.StartDate && leave.StartDate <= l.EndDate) ||
                                (leave.EndDate >= l.StartDate && leave.EndDate <= l.EndDate)))
                    .FirstOrDefaultAsync();

                if (existingLeave != null)
                {
                    ModelState.AddModelError("", "You already have a leave request for these dates.");
                    
                    // Reload leave balance for view
                    ViewBag.LeaveBalance = leaveBalance;
                    return View(leave);
                }

                leave.EmployeeId = employee.EmployeeId;
                leave.AppliedOn = DateTime.Now;
                leave.Status = LeaveStatus.Pending;
                leave.NumberOfDays = leave.IsHalfDay ? 1 : (leave.EndDate - leave.StartDate).Days + 1;

                _context.Leaves.Add(leave);
                await _context.SaveChangesAsync();

                // Send notification email to admin
                try
                {
                    // Get admin email from configuration or database
                    var adminEmail = "admin@payrollpro.com"; // You can get this from appsettings.json or database
                    
                    await _emailService.SendEmailAsync(
                        adminEmail,
                        "New Leave Request - PayRoll Pro",
                        GetAdminLeaveNotificationTemplate(employee.Name, employee.EmployeeCode, leave)
                    );
                }
                catch (Exception ex)
                {
                    // Log email error but don't fail the leave application
                    Console.WriteLine($"Failed to send admin notification: {ex.Message}");
                }

                TempData["Success"] = "Leave request submitted successfully!";
                return RedirectToAction(nameof(LeaveRequests));
            }

            // Reload leave balance for view if validation failed
            var leaveBalanceView = await GetOrCreateLeaveBalance(employee.EmployeeId, DateTime.Now.Year);
            ViewBag.LeaveBalance = leaveBalanceView;
            return View(leave);
        }

        // POST: EmployeePortal/CancelLeave/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelLeave(int id)
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            var leave = await _context.Leaves
                .FirstOrDefaultAsync(l => l.LeaveId == id && l.EmployeeId == employee.EmployeeId);

            if (leave == null)
            {
                return Json(new { success = false, message = "Leave request not found." });
            }

            if (leave.Status != LeaveStatus.Pending)
            {
                return Json(new { success = false, message = "Only pending leave requests can be cancelled." });
            }

            leave.Status = LeaveStatus.Cancelled;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Leave request cancelled successfully!" });
        }

        // GET: EmployeePortal/LeaveBalance
        public async Task<IActionResult> LeaveBalance()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToAction("LinkAccount");
            }

            var leaveBalance = await GetOrCreateLeaveBalance(employee.EmployeeId, DateTime.Now.Year);
            
            // Pass both employee and leave balance to view
            ViewBag.Employee = employee;

            return View(leaveBalance);
        }

        // GET: EmployeePortal/LinkAccount
        [AllowAnonymous]
        public IActionResult LinkAccount()
        {
            return View();
        }

        // POST: EmployeePortal/LinkAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkAccount(string employeeCode, string email)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Find employee by code and email
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode && e.Email == email);

            if (employee == null)
            {
                ModelState.AddModelError("", "No employee found with the provided code and email.");
                return View();
            }

            // Check if already linked
            if (!string.IsNullOrEmpty(employee.UserId))
            {
                if (employee.UserId == user.Id)
                {
                    TempData["Success"] = "Your account is already linked!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "This employee is already linked to another account.");
                    return View();
                }
            }

            // Link the employee to the user
            employee.UserId = user.Id;
            await _context.SaveChangesAsync();

            // Add Employee role if not already
            if (!await _userManager.IsInRoleAsync(user, "Employee"))
            {
                await _userManager.AddToRoleAsync(user, "Employee");
            }

            TempData["Success"] = "Account linked successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Helper method to get current logged-in employee
        private async Task<Employee?> GetCurrentEmployeeAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .Include(e => e.ShiftNavigation)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);
        }

        private async Task<LeaveBalance> GetOrCreateLeaveBalance(int employeeId, int year)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId && lb.Year == year);

            if (leaveBalance == null)
            {
                // Get employee to check gender for maternity leave
                var employee = await _context.Employees.FindAsync(employeeId);
                
                // Create default leave balance for new year
                leaveBalance = new LeaveBalance
                {
                    EmployeeId = employeeId,
                    Year = year,
                    CasualLeaveBalance = 12, // Default: 12 days
                    SickLeaveBalance = 10,    // Default: 10 days
                    AnnualLeaveBalance = 20,  // Default: 20 days
                    MaternityLeaveBalance = (employee?.Gender == Gender.Female) ? 90 : 0, // 90 days for females, 0 for others
                    CasualLeaveUsed = 0,
                    SickLeaveUsed = 0,
                    AnnualLeaveUsed = 0,
                    MaternityLeaveUsed = 0,
                    CreatedAt = DateTime.Now
                };

                _context.LeaveBalances.Add(leaveBalance);
                await _context.SaveChangesAsync();
            }

            return leaveBalance;
        }

        private bool CheckLeaveBalance(LeaveBalance leaveBalance, LeaveType leaveType, decimal days)
        {
            return leaveType switch
            {
                LeaveType.CasualLeave => (leaveBalance.CasualLeaveBalance - leaveBalance.CasualLeaveUsed) >= days,
                LeaveType.SickLeave => (leaveBalance.SickLeaveBalance - leaveBalance.SickLeaveUsed) >= days,
                LeaveType.AnnualLeave => (leaveBalance.AnnualLeaveBalance - leaveBalance.AnnualLeaveUsed) >= days,
                LeaveType.MaternityLeave => (leaveBalance.MaternityLeaveBalance - leaveBalance.MaternityLeaveUsed) >= days,
                LeaveType.UnpaidLeave => true, // Always allow unpaid leave
                _ => false
            };
        }

        private decimal GetRemainingLeaveBalance(LeaveBalance leaveBalance, LeaveType leaveType)
        {
            return leaveType switch
            {
                LeaveType.CasualLeave => leaveBalance.CasualLeaveBalance - leaveBalance.CasualLeaveUsed,
                LeaveType.SickLeave => leaveBalance.SickLeaveBalance - leaveBalance.SickLeaveUsed,
                LeaveType.AnnualLeave => leaveBalance.AnnualLeaveBalance - leaveBalance.AnnualLeaveUsed,
                LeaveType.MaternityLeave => leaveBalance.MaternityLeaveBalance - leaveBalance.MaternityLeaveUsed,
                _ => 0
            };
        }

        // Helper method to generate admin notification email template
        private string GetAdminLeaveNotificationTemplate(string employeeName, string employeeCode, Leave leave)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f59e0b; }}
        .footer {{ text-align: center; color: #5a6c7d; font-size: 12px; margin-top: 20px; }}
        .btn {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; text-decoration: none; border-radius: 8px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? New Leave Request</h1>
        </div>
        <div class='content'>
            <p><strong>A new leave request requires your attention!</strong></p>
            
            <div class='info-box'>
                <p><strong>Employee:</strong> {employeeName} ({employeeCode})</p>
                <p><strong>Leave Type:</strong> {leave.LeaveType}</p>
                <p><strong>Start Date:</strong> {leave.StartDate:MMMM dd, yyyy}</p>
                <p><strong>End Date:</strong> {leave.EndDate:MMMM dd, yyyy}</p>
                <p><strong>Duration:</strong> {leave.NumberOfDays} day(s)</p>
                <p><strong>Reason:</strong> {leave.Reason}</p>
                <p><strong>Applied On:</strong> {leave.AppliedOn:MMMM dd, yyyy HH:mm}</p>
            </div>
            
            <p>Please login to the admin portal to approve or reject this leave request.</p>
            
            <div style='text-align: center;'>
                <a href='#' class='btn'>Review Leave Request</a>
            </div>
            
            <div class='footer'>
                <p>&copy; 2025 PayRoll Pro. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}
