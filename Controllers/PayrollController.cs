using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;
using System.Security.Claims;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payroll
        public async Task<IActionResult> Index(int? month, int? year, string status, string searchString)
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            ViewData["CurrentMonth"] = month ?? currentMonth;
            ViewData["CurrentYear"] = year ?? currentYear;
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentFilter"] = searchString;

            var payrolls = _context.Payrolls
                .Include(p => p.Employee)
                    .ThenInclude(e => e.DepartmentNavigation)
                .Include(p => p.Employee)
                    .ThenInclude(e => e.DesignationNavigation)
                .Include(p => p.PayrollDetails)
                .AsQueryable();

            // Filter by month
            if (month.HasValue)
            {
                payrolls = payrolls.Where(p => p.Month == month.Value);
            }

            // Filter by year
            if (year.HasValue)
            {
                payrolls = payrolls.Where(p => p.Year == year.Value);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PayrollStatus>(status, out var statusEnum))
            {
                payrolls = payrolls.Where(p => p.Status == statusEnum);
            }

            // Search by employee name or payroll number
            if (!string.IsNullOrEmpty(searchString))
            {
                payrolls = payrolls.Where(p => p.Employee.Name.Contains(searchString) ||
                                               p.PayrollNumber.Contains(searchString) ||
                                               p.Employee.EmployeeCode.Contains(searchString));
            }

            return View(await payrolls.OrderByDescending(p => p.Year)
                                     .ThenByDescending(p => p.Month)
                                     .ThenBy(p => p.Employee.Name)
                                     .ToListAsync());
        }

        // GET: Payroll/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                    .ThenInclude(e => e.DepartmentNavigation)
                .Include(p => p.Employee)
                    .ThenInclude(e => e.DesignationNavigation)
                .Include(p => p.PayrollDetails)
                    .ThenInclude(pd => pd.AllowanceDeduction)
                .FirstOrDefaultAsync(m => m.PayrollId == id);

            if (payroll == null)
            {
                return NotFound();
            }

            return View(payroll);
        }

        // GET: Payroll/Generate
        public IActionResult Generate()
        {
            ViewBag.Employees = new SelectList(_context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.Name), "EmployeeId", "Name");

            ViewData["Month"] = DateTime.Now.Month;
            ViewData["Year"] = DateTime.Now.Year;

            return View();
        }

        // POST: Payroll/GenerateSingle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSingle(int employeeId, int month, int year)
        {
            // Check if payroll already exists
            var existing = await _context.Payrolls
                .AnyAsync(p => p.EmployeeId == employeeId && p.Month == month && p.Year == year);

            if (existing)
            {
                TempData["Error"] = "Payroll for this employee and period already exists!";
                return RedirectToAction(nameof(Generate));
            }

            var employee = await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found!";
                return RedirectToAction(nameof(Generate));
            }

            var payroll = await CalculatePayroll(employee, month, year);
            
            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Payroll generated successfully for {employee.Name} - {payroll.PayPeriod}!";
            return RedirectToAction(nameof(Details), new { id = payroll.PayrollId });
        }

        // POST: Payroll/GenerateBulk
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBulk(int month, int year)
        {
            var employees = await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .Where(e => e.Status == EmploymentStatus.Active)
                .ToListAsync();

            if (!employees.Any())
            {
                TempData["Error"] = "No active employees found!";
                return RedirectToAction(nameof(Generate));
            }

            // Check for existing payrolls
            var existingEmployeeIds = await _context.Payrolls
                .Where(p => p.Month == month && p.Year == year)
                .Select(p => p.EmployeeId)
                .ToListAsync();

            var employeesToProcess = employees.Where(e => !existingEmployeeIds.Contains(e.EmployeeId)).ToList();

            if (!employeesToProcess.Any())
            {
                TempData["Error"] = "Payroll already exists for all active employees for this period!";
                return RedirectToAction(nameof(Generate));
            }

            int successCount = 0;
            var payrolls = new List<Payroll>();

            foreach (var employee in employeesToProcess)
            {
                try
                {
                    var payroll = await CalculatePayroll(employee, month, year);
                    payrolls.Add(payroll);
                    successCount++;
                }
                catch (Exception)
                {
                    // Log error but continue processing
                    continue;
                }
            }

            if (payrolls.Any())
            {
                _context.Payrolls.AddRange(payrolls);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Bulk payroll generated successfully for {successCount} employee(s)!";
            return RedirectToAction(nameof(Index), new { month, year });
        }

        // GET: Payroll/Approve/5
        public async Task<IActionResult> Approve(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .Include(p => p.PayrollDetails)
                .FirstOrDefaultAsync(p => p.PayrollId == id);

            if (payroll == null)
            {
                return NotFound();
            }

            if (payroll.Status != PayrollStatus.Pending && payroll.Status != PayrollStatus.Draft)
            {
                TempData["Error"] = "Only pending or draft payrolls can be approved!";
                return RedirectToAction(nameof(Details), new { id });
            }

            payroll.Status = PayrollStatus.Approved;
            payroll.ApprovedBy = User.Identity?.Name ?? "System";
            payroll.ApprovedAt = DateTime.Now;
            payroll.UpdatedBy = User.Identity?.Name ?? "System";
            payroll.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Payroll approved successfully for {payroll.Employee.Name}!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Payroll/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.PayrollId == id);

            if (payroll == null)
            {
                return Json(new { success = false, message = "Payroll not found." });
            }

            if (payroll.Status == PayrollStatus.Approved || payroll.Status == PayrollStatus.Paid)
            {
                return Json(new { success = false, message = "Cannot delete approved or paid payroll!" });
            }

            _context.Payrolls.Remove(payroll);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Payroll for {payroll.Employee.Name} deleted successfully!" });
        }

        // Helper method to calculate payroll
        private async Task<Payroll> CalculatePayroll(Employee employee, int month, int year)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
            var userName = User.Identity?.Name ?? "System";

            // Calculate attendance
            var firstDayOfMonth = new DateTime(year, month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employee.EmployeeId &&
                           a.Date >= firstDayOfMonth &&
                           a.Date <= lastDayOfMonth)
                .ToListAsync();

            int totalWorkingDays = (lastDayOfMonth - firstDayOfMonth).Days + 1;
            int presentDays = attendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
            int absentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent);
            int lateDays = attendances.Count(a => a.Status == AttendanceStatus.Late);

            // Calculate leave days
            var leaves = await _context.Leaves
                .Where(l => l.EmployeeId == employee.EmployeeId &&
                           l.Status == LeaveStatus.Approved &&
                           ((l.StartDate >= firstDayOfMonth && l.StartDate <= lastDayOfMonth) ||
                            (l.EndDate >= firstDayOfMonth && l.EndDate <= lastDayOfMonth) ||
                            (l.StartDate <= firstDayOfMonth && l.EndDate >= lastDayOfMonth)))
                .ToListAsync();

            int leaveDays = 0;
            int paidLeaves = 0;
            int unpaidLeaves = 0;

            foreach (var leave in leaves)
            {
                var leaveStart = leave.StartDate < firstDayOfMonth ? firstDayOfMonth : leave.StartDate;
                var leaveEnd = leave.EndDate > lastDayOfMonth ? lastDayOfMonth : leave.EndDate;
                int days = (int)(leaveEnd - leaveStart).TotalDays + 1;

                leaveDays += days;
                if (leave.LeaveType == LeaveType.CasualLeave || leave.LeaveType == LeaveType.SickLeave || leave.LeaveType == LeaveType.AnnualLeave)
                {
                    paidLeaves += days;
                }
                else
                {
                    unpaidLeaves += days;
                }
            }

            // Calculate basic salary (prorated for unpaid leaves/absences)
            decimal basicSalary = employee.BasicSalary;
            int effectiveWorkingDays = presentDays + paidLeaves;
            decimal perDaySalary = basicSalary / totalWorkingDays;
            decimal leaveDeduction = (unpaidLeaves + absentDays) * perDaySalary;

            // Get active allowances and deductions
            var activeComponents = await _context.AllowanceDeductions
                .Where(c => c.Status == ComponentStatus.Active)
                .ToListAsync();

            decimal totalAllowances = 0;
            decimal totalDeductions = 0;
            var payrollDetails = new List<PayrollDetail>();

            foreach (var component in activeComponents)
            {
                decimal amount = CalculateComponentAmount(component, basicSalary, basicSalary + totalAllowances);

                var detail = new PayrollDetail
                {
                    AllowanceDeductionId = component.AllowanceDeductionId,
                    ComponentName = component.Name,
                    ComponentType = component.Type,
                    CalculationMethod = component.CalculationMethod,
                    ComponentValue = component.Value,
                    Amount = amount,
                    IsTaxable = component.IsTaxable
                };

                payrollDetails.Add(detail);

                if (component.Type == ComponentType.Allowance)
                {
                    totalAllowances += amount;
                }
                else
                {
                    totalDeductions += amount;
                }
            }

            // Add leave deduction
            totalDeductions += leaveDeduction;

            decimal grossSalary = basicSalary + totalAllowances;
            decimal netSalary = grossSalary - totalDeductions;

            var payroll = new Payroll
            {
                PayrollNumber = GeneratePayrollNumber(employee, month, year),
                EmployeeId = employee.EmployeeId,
                Month = month,
                Year = year,
                PaymentDate = lastDayOfMonth,
                BasicSalary = basicSalary,
                TotalAllowances = totalAllowances,
                TotalDeductions = totalDeductions,
                GrossSalary = grossSalary,
                NetSalary = netSalary,
                TotalWorkingDays = totalWorkingDays,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                LeaveDays = leaveDays,
                PaidLeaves = paidLeaves,
                UnpaidLeaves = unpaidLeaves,
                LateDays = lateDays,
                LeaveDeductionAmount = leaveDeduction,
                OvertimeHours = 0,
                OvertimeAmount = 0,
                Status = PayrollStatus.Pending,
                CreatedBy = userName,
                CreatedAt = DateTime.Now,
                PayrollDetails = payrollDetails
            };

            return payroll;
        }

        private decimal CalculateComponentAmount(AllowanceDeduction component, decimal basicSalary, decimal grossSalary)
        {
            decimal amount = 0;

            switch (component.CalculationMethod)
            {
                case CalculationMethod.FixedAmount:
                    amount = component.Value;
                    break;
                case CalculationMethod.PercentageOfBasic:
                    amount = (basicSalary * component.Value) / 100;
                    break;
                case CalculationMethod.PercentageOfGross:
                    amount = (grossSalary * component.Value) / 100;
                    break;
            }

            // Apply maximum cap
            if (component.MaximumCap.HasValue && amount > component.MaximumCap.Value)
            {
                amount = component.MaximumCap.Value;
            }

            return amount;
        }

        private string GeneratePayrollNumber(Employee employee, int month, int year)
        {
            return $"PAY-{year}{month:D2}-{employee.EmployeeCode}";
        }
    }
}
