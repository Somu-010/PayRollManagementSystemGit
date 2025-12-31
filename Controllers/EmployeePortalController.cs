using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]  // Allow any authenticated user initially
    public class EmployeePortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EmployeePortalController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            // Calculate stats
            ViewBag.TotalDays = (lastDayOfMonth - firstDayOfMonth).Days + 1;
            ViewBag.PresentDays = attendanceList.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
            ViewBag.AbsentDays = attendanceList.Count(a => a.Status == AttendanceStatus.Absent);
            ViewBag.LateDays = attendanceList.Count(a => a.IsLate);
            ViewBag.LeaveDays = attendanceList.Count(a => a.Status == AttendanceStatus.OnLeave);
            ViewBag.TotalHours = attendanceList.Sum(a => a.TotalHours ?? 0);
            ViewBag.OvertimeHours = attendanceList.Sum(a => a.OvertimeHours ?? 0);

            return View(attendanceList);
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
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employee.EmployeeId && lb.Year == DateTime.Now.Year);
            ViewBag.LeaveBalance = leaveBalance;

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
                    return View(leave);
                }

                if (leave.StartDate < DateTime.Today)
                {
                    ModelState.AddModelError("StartDate", "Cannot apply leave for past dates.");
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
                    return View(leave);
                }

                leave.EmployeeId = employee.EmployeeId;
                leave.AppliedOn = DateTime.Now;
                leave.Status = LeaveStatus.Pending;
                leave.NumberOfDays = leave.IsHalfDay ? 1 : (leave.EndDate - leave.StartDate).Days + 1;

                _context.Leaves.Add(leave);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Leave request submitted successfully!";
                return RedirectToAction(nameof(LeaveRequests));
            }

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

            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employee.EmployeeId && lb.Year == DateTime.Now.Year);

            if (leaveBalance == null)
            {
                // Create default leave balance if not exists
                leaveBalance = new LeaveBalance
                {
                    EmployeeId = employee.EmployeeId,
                    Year = DateTime.Now.Year,
                    AnnualLeaveBalance = 20,
                    SickLeaveBalance = 10,
                    CasualLeaveBalance = 5,
                    MaternityLeaveBalance = 90,
                    AnnualLeaveUsed = 0,
                    SickLeaveUsed = 0,
                    CasualLeaveUsed = 0,
                    MaternityLeaveUsed = 0,
                    CreatedAt = DateTime.Now
                };
                _context.LeaveBalances.Add(leaveBalance);
                await _context.SaveChangesAsync();
            }

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
    }
}
