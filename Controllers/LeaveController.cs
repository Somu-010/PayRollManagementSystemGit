using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Leave
        public async Task<IActionResult> Index(string searchString, string leaveType, string status)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentLeaveType"] = leaveType;
            ViewData["CurrentStatus"] = status;

            var leaves = from l in _context.Leaves
                         .Include(l => l.Employee)
                         .ThenInclude(e => e.DepartmentNavigation)
                         select l;

            // Search by employee name or code
            if (!string.IsNullOrEmpty(searchString))
            {
                leaves = leaves.Where(l => l.Employee!.Name.Contains(searchString)
                                       || l.Employee!.EmployeeCode.Contains(searchString));
            }

            // Filter by leave type
            if (!string.IsNullOrEmpty(leaveType) && Enum.TryParse<LeaveType>(leaveType, out var leaveTypeEnum))
            {
                leaves = leaves.Where(l => l.LeaveType == leaveTypeEnum);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveStatus>(status, out var statusEnum))
            {
                leaves = leaves.Where(l => l.Status == statusEnum);
            }

            return View(await leaves.OrderByDescending(l => l.AppliedOn).ToListAsync());
        }

        // GET: Leave/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .ThenInclude(e => e.DepartmentNavigation)
                .FirstOrDefaultAsync(m => m.LeaveId == id);

            if (leave == null)
            {
                return NotFound();
            }

            return View(leave);
        }

        // GET: Leave/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdownData();
            return View();
        }

        // POST: Leave/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeId,LeaveType,StartDate,EndDate,Reason,IsHalfDay")] Leave leave)
        {
            ModelState.Remove("Employee");

            if (ModelState.IsValid)
            {
                // Validate dates
                if (leave.EndDate < leave.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after or equal to start date.");
                    await LoadDropdownData();
                    return View(leave);
                }

                // Calculate number of days
                if (leave.IsHalfDay)
                {
                    leave.NumberOfDays = 0; // Will be set to 0.5
                }
                else
                {
                    leave.NumberOfDays = (leave.EndDate - leave.StartDate).Days + 1;
                }

                // Check leave balance
                var leaveBalance = await GetOrCreateLeaveBalance(leave.EmployeeId, DateTime.Now.Year);
                var hasBalance = CheckLeaveBalance(leaveBalance, leave.LeaveType, leave.IsHalfDay ? 0.5m : leave.NumberOfDays);

                if (!hasBalance)
                {
                    ModelState.AddModelError("", $"Insufficient {leave.LeaveType} balance. Please check your leave balance.");
                    await LoadDropdownData();
                    return View(leave);
                }

                leave.Status = LeaveStatus.Pending;
                leave.AppliedOn = DateTime.Now;
                leave.CreatedAt = DateTime.Now;

                _context.Add(leave);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Leave application submitted successfully!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownData();
            return View(leave);
        }

        // GET: Get leave data for editing (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetLeave(int id)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveId == id);

            if (leave == null)
            {
                return NotFound();
            }

            return Json(new
            {
                leaveId = leave.LeaveId,
                employeeId = leave.EmployeeId,
                employeeName = leave.Employee?.Name,
                leaveType = leave.LeaveType.ToString(),
                startDate = leave.StartDate.ToString("yyyy-MM-dd"),
                endDate = leave.EndDate.ToString("yyyy-MM-dd"),
                numberOfDays = leave.NumberOfDays,
                reason = leave.Reason,
                isHalfDay = leave.IsHalfDay,
                status = leave.Status.ToString()
            });
        }

        // POST: Leave/Edit (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] Leave leave)
        {
            ModelState.Remove("Employee");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingLeave = await _context.Leaves.FindAsync(leave.LeaveId);
                    if (existingLeave == null)
                    {
                        return Json(new { success = false, message = "Leave record not found." });
                    }

                    // Only allow editing if leave is pending
                    if (existingLeave.Status != LeaveStatus.Pending)
                    {
                        return Json(new { success = false, message = "Cannot edit leave that has been approved/rejected." });
                    }

                    // Validate dates
                    if (leave.EndDate < leave.StartDate)
                    {
                        return Json(new { success = false, message = "End date must be after or equal to start date." });
                    }

                    // Calculate number of days
                    if (leave.IsHalfDay)
                    {
                        leave.NumberOfDays = 0;
                    }
                    else
                    {
                        leave.NumberOfDays = (leave.EndDate - leave.StartDate).Days + 1;
                    }

                    existingLeave.LeaveType = leave.LeaveType;
                    existingLeave.StartDate = leave.StartDate;
                    existingLeave.EndDate = leave.EndDate;
                    existingLeave.NumberOfDays = leave.NumberOfDays;
                    existingLeave.Reason = leave.Reason;
                    existingLeave.IsHalfDay = leave.IsHalfDay;
                    existingLeave.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Leave application updated successfully!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeaveExists(leave.LeaveId))
                    {
                        return Json(new { success = false, message = "Leave record not found." });
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

        // POST: Leave/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string adminRemarks)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveId == id);

            if (leave == null)
            {
                return Json(new { success = false, message = "Leave record not found." });
            }

            if (leave.Status != LeaveStatus.Pending)
            {
                return Json(new { success = false, message = "Leave has already been processed." });
            }

            // Update leave balance
            var leaveBalance = await GetOrCreateLeaveBalance(leave.EmployeeId, leave.StartDate.Year);
            var leaveDays = leave.IsHalfDay ? 0.5m : leave.NumberOfDays;

            UpdateLeaveBalance(leaveBalance, leave.LeaveType, leaveDays);

            leave.Status = LeaveStatus.Approved;
            leave.AdminRemarks = adminRemarks;
            leave.ApprovedBy = User.Identity?.Name ?? "Admin";
            leave.ActionDate = DateTime.Now;
            leave.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Leave approved for {leave.Employee?.Name}!" });
        }

        // POST: Leave/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string adminRemarks)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveId == id);

            if (leave == null)
            {
                return Json(new { success = false, message = "Leave record not found." });
            }

            if (leave.Status != LeaveStatus.Pending)
            {
                return Json(new { success = false, message = "Leave has already been processed." });
            }

            leave.Status = LeaveStatus.Rejected;
            leave.AdminRemarks = adminRemarks;
            leave.ApprovedBy = User.Identity?.Name ?? "Admin";
            leave.ActionDate = DateTime.Now;
            leave.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Leave rejected for {leave.Employee?.Name}." });
        }

        // POST: Leave/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var leave = await _context.Leaves.FindAsync(id);

            if (leave == null)
            {
                return Json(new { success = false, message = "Leave record not found." });
            }

            if (leave.Status != LeaveStatus.Pending)
            {
                return Json(new { success = false, message = "Can only cancel pending leave applications." });
            }

            leave.Status = LeaveStatus.Cancelled;
            leave.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Leave application cancelled successfully!" });
        }

        // POST: Leave/Delete (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LeaveId == id);

            if (leave == null)
            {
                return Json(new { success = false, message = "Leave record not found." });
            }

            var employeeName = leave.Employee?.Name ?? "Employee";

            _context.Leaves.Remove(leave);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Leave record for {employeeName} deleted successfully!" });
        }

        // GET: Leave/LeaveBalance
        public async Task<IActionResult> LeaveBalance(int? employeeId)
        {
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

            var currentYear = DateTime.Now.Year;
            var leaveBalance = await GetOrCreateLeaveBalance(employeeId.Value, currentYear);

            // Get leave history for current year
            var leaveHistory = await _context.Leaves
                .Where(l => l.EmployeeId == employeeId && l.StartDate.Year == currentYear)
                .OrderByDescending(l => l.AppliedOn)
                .ToListAsync();

            var viewModel = new
            {
                Employee = employee,
                LeaveBalance = leaveBalance,
                LeaveHistory = leaveHistory,
                Year = currentYear
            };

            return View(viewModel);
        }

        // GET: Leave/Report
        public async Task<IActionResult> Report(int? month, int? year)
        {
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            ViewBag.SelectedMonth = currentMonth;
            ViewBag.SelectedYear = currentYear;

            var startDate = new DateTime(currentYear, currentMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var leaves = await _context.Leaves
                .Include(l => l.Employee)
                .ThenInclude(e => e.DepartmentNavigation)
                .Where(l => l.StartDate >= startDate && l.StartDate <= endDate)
                .OrderBy(l => l.StartDate)
                .ToListAsync();

            // Calculate summary
            var summary = new
            {
                Month = startDate.ToString("MMMM yyyy"),
                TotalLeaves = leaves.Count,
                PendingLeaves = leaves.Count(l => l.Status == LeaveStatus.Pending),
                ApprovedLeaves = leaves.Count(l => l.Status == LeaveStatus.Approved),
                RejectedLeaves = leaves.Count(l => l.Status == LeaveStatus.Rejected),
                TotalLeaveDays = leaves.Where(l => l.Status == LeaveStatus.Approved).Sum(l => l.NumberOfDays),
                Leaves = leaves
            };

            return View(summary);
        }
        // GET: API endpoint for leave balance (for AJAX calls)
        [HttpGet]
        [Route("/api/GetLeaveBalance/{employeeId}")]
        public async Task<IActionResult> GetLeaveBalance(int employeeId)
        {
            var currentYear = DateTime.Now.Year;
            var leaveBalance = await GetOrCreateLeaveBalance(employeeId, currentYear);

            return Json(new
            {
                hasBalance = true,
                casualLeave = leaveBalance.RemainingCasualLeave,
                sickLeave = leaveBalance.RemainingSickLeave,
                annualLeave = leaveBalance.RemainingAnnualLeave,
                maternityLeave = leaveBalance.RemainingMaternityLeave
            });
        }

        private bool LeaveExists(int id)
        {
            return _context.Leaves.Any(e => e.LeaveId == id);
        }

        private async Task LoadDropdownData()
        {
            ViewBag.Employees = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.Name)
                .Select(e => new { e.EmployeeId, e.Name, e.EmployeeCode })
                .ToListAsync();
        }

        private async Task<LeaveBalance> GetOrCreateLeaveBalance(int employeeId, int year)
        {
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId && lb.Year == year);

            if (leaveBalance == null)
            {
                // Create default leave balance for new year
                leaveBalance = new LeaveBalance
                {
                    EmployeeId = employeeId,
                    Year = year,
                    CasualLeaveBalance = 12, // Default: 12 days
                    SickLeaveBalance = 10,    // Default: 10 days
                    AnnualLeaveBalance = 20,  // Default: 20 days
                    MaternityLeaveBalance = 90, // Default: 90 days
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

        private void UpdateLeaveBalance(LeaveBalance leaveBalance, LeaveType leaveType, decimal days)
        {
            switch (leaveType)
            {
                case LeaveType.CasualLeave:
                    leaveBalance.CasualLeaveUsed += days;
                    break;
                case LeaveType.SickLeave:
                    leaveBalance.SickLeaveUsed += days;
                    break;
                case LeaveType.AnnualLeave:
                    leaveBalance.AnnualLeaveUsed += days;
                    break;
                case LeaveType.MaternityLeave:
                    leaveBalance.MaternityLeaveUsed += days;
                    break;
            }

            leaveBalance.UpdatedAt = DateTime.Now;
        }
    }
}