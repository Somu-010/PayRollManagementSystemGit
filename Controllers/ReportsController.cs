using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        #region Employee List Report

        // GET: Reports/EmployeeList
        public async Task<IActionResult> EmployeeList(string? department, string? designation, string? status, string? searchString)
        {
            ViewData["CurrentDepartment"] = department;
            ViewData["CurrentDesignation"] = designation;
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentFilter"] = searchString;

            // Get distinct departments and designations for filters
            ViewBag.Departments = await _context.Departments
                .Where(d => d.Status == DepartmentStatus.Active)
                .OrderBy(d => d.Name)
                .Select(d => d.Name)
                .Distinct()
                .ToListAsync();

            ViewBag.Designations = await _context.Designations
                .Where(d => d.Status == DesignationStatus.Active)
                .OrderBy(d => d.Title)
                .Select(d => d.Title)
                .Distinct()
                .ToListAsync();

            var employees = _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .Include(e => e.ShiftNavigation)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(department))
            {
                employees = employees.Where(e => e.DepartmentNavigation!.Name == department);
            }

            if (!string.IsNullOrEmpty(designation))
            {
                employees = employees.Where(e => e.DesignationNavigation!.Title == designation);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<EmploymentStatus>(status, out var statusEnum))
            {
                employees = employees.Where(e => e.Status == statusEnum);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e => 
                    e.Name.Contains(searchString) || 
                    e.EmployeeCode.Contains(searchString) ||
                    e.Email.Contains(searchString));
            }

            var employeeList = await employees
                .OrderBy(e => e.Name)
                .ToListAsync();

            // Calculate summary statistics
            ViewBag.TotalEmployees = employeeList.Count;
            ViewBag.TotalSalary = employeeList.Sum(e => e.BasicSalary);
            ViewBag.AverageSalary = employeeList.Any() ? employeeList.Average(e => e.BasicSalary) : 0;
            ViewBag.ActiveEmployees = employeeList.Count(e => e.Status == EmploymentStatus.Active);

            return View(employeeList);
        }

        #endregion

        #region Attendance Reports

        // GET: Reports/DailyAttendance
        public async Task<IActionResult> DailyAttendance(DateTime? date, string? department, string? status)
        {
            var selectedDate = date ?? DateTime.Today;
            ViewData["SelectedDate"] = selectedDate;
            ViewData["CurrentDepartment"] = department;
            ViewData["CurrentStatus"] = status;

            // Get distinct departments for filter
            ViewBag.Departments = await _context.Departments
                .Where(d => d.Status == DepartmentStatus.Active)
                .OrderBy(d => d.Name)
                .Select(d => d.Name)
                .Distinct()
                .ToListAsync();

            var attendances = _context.Attendances
                .Include(a => a.Employee)
                    .ThenInclude(e => e!.DepartmentNavigation)
                .Include(a => a.Employee)
                    .ThenInclude(e => e!.DesignationNavigation)
                .Where(a => a.Date == selectedDate)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(department))
            {
                attendances = attendances.Where(a => a.Employee!.DepartmentNavigation!.Name == department);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AttendanceStatus>(status, out var statusEnum))
            {
                attendances = attendances.Where(a => a.Status == statusEnum);
            }

            var attendanceList = await attendances
                .OrderBy(a => a.Employee!.Name)
                .ToListAsync();

            // Get all active employees for the day
            var allActiveEmployees = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .CountAsync();

            // Calculate statistics
            ViewBag.TotalEmployees = allActiveEmployees;
            ViewBag.PresentCount = attendanceList.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
            ViewBag.AbsentCount = attendanceList.Count(a => a.Status == AttendanceStatus.Absent);
            ViewBag.LateCount = attendanceList.Count(a => a.IsLate);
            ViewBag.OnLeaveCount = attendanceList.Count(a => a.Status == AttendanceStatus.OnLeave);
            ViewBag.HalfDayCount = attendanceList.Count(a => a.IsHalfDay);
            ViewBag.AttendancePercentage = allActiveEmployees > 0 
                ? Math.Round((decimal)ViewBag.PresentCount / allActiveEmployees * 100, 2) 
                : 0;

            return View(attendanceList);
        }

        // GET: Reports/MonthlyAttendance
        public async Task<IActionResult> MonthlyAttendance(int? month, int? year, int? employeeId, string? department)
        {
            var selectedMonth = month ?? DateTime.Now.Month;
            var selectedYear = year ?? DateTime.Now.Year;

            ViewData["SelectedMonth"] = selectedMonth;
            ViewData["SelectedYear"] = selectedYear;
            ViewData["SelectedEmployee"] = employeeId;
            ViewData["CurrentDepartment"] = department;

            // Get employees for dropdown
            ViewBag.Employees = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.Name)
                .ToListAsync();

            // Get departments for filter
            ViewBag.Departments = await _context.Departments
                .Where(d => d.Status == DepartmentStatus.Active)
                .OrderBy(d => d.Name)
                .Select(d => d.Name)
                .Distinct()
                .ToListAsync();

            var firstDayOfMonth = new DateTime(selectedYear, selectedMonth, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var query = _context.Attendances
                .Include(a => a.Employee)
                    .ThenInclude(e => e!.DepartmentNavigation)
                .Include(a => a.Employee)
                    .ThenInclude(e => e!.DesignationNavigation)
                .Where(a => a.Date >= firstDayOfMonth && a.Date <= lastDayOfMonth)
                .AsQueryable();

            // Apply filters
            if (employeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(a => a.Employee!.DepartmentNavigation!.Name == department);
            }

            var attendances = await query
                .OrderBy(a => a.Employee!.Name)
                .ThenBy(a => a.Date)
                .ToListAsync();

            // Group by employee for summary - with null checks
            var employeeSummary = attendances
                .Where(a => a.Employee != null) // Filter out null employees
                .GroupBy(a => a.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    EmployeeName = g.First().Employee?.Name ?? "Unknown",
                    EmployeeCode = g.First().Employee?.EmployeeCode ?? "N/A",
                    Department = g.First().Employee?.DepartmentNavigation?.Name ?? "N/A",
                    Designation = g.First().Employee?.DesignationNavigation?.Title ?? "N/A",
                    TotalDays = g.Count(),
                    PresentDays = g.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late),
                    AbsentDays = g.Count(a => a.Status == AttendanceStatus.Absent),
                    LateDays = g.Count(a => a.IsLate),
                    OnLeaveDays = g.Count(a => a.Status == AttendanceStatus.OnLeave),
                    HalfDays = g.Count(a => a.IsHalfDay),
                    TotalHours = g.Sum(a => a.TotalHours ?? 0),
                    OvertimeHours = g.Sum(a => a.OvertimeHours ?? 0)
                })
                .ToList();

            ViewBag.EmployeeSummary = employeeSummary;
            ViewBag.WorkingDays = (lastDayOfMonth - firstDayOfMonth).Days + 1;

            return View(attendances);
        }

        #endregion

        #region Payroll Reports

        // GET: Reports/PayrollReport
        public async Task<IActionResult> PayrollReport(int? month, int? year, string? department, string? status)
        {
            var selectedMonth = month ?? DateTime.Now.Month;
            var selectedYear = year ?? DateTime.Now.Year;

            ViewData["SelectedMonth"] = selectedMonth;
            ViewData["SelectedYear"] = selectedYear;
            ViewData["CurrentDepartment"] = department;
            ViewData["CurrentStatus"] = status;

            // Get departments for filter
            ViewBag.Departments = await _context.Departments
                .Where(d => d.Status == DepartmentStatus.Active)
                .OrderBy(d => d.Name)
                .Select(d => d.Name)
                .Distinct()
                .ToListAsync();

            var payrolls = _context.Payrolls
                .Include(p => p.Employee)
                    .ThenInclude(e => e!.DepartmentNavigation)
                .Include(p => p.Employee)
                    .ThenInclude(e => e!.DesignationNavigation)
                .Include(p => p.PayrollDetails)
                .Where(p => p.Month == selectedMonth && p.Year == selectedYear)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(department))
            {
                payrolls = payrolls.Where(p => p.Employee!.DepartmentNavigation!.Name == department);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PayrollStatus>(status, out var statusEnum))
            {
                payrolls = payrolls.Where(p => p.Status == statusEnum);
            }

            var payrollList = await payrolls
                .OrderBy(p => p.Employee!.Name)
                .ToListAsync();

            // Calculate totals
            ViewBag.TotalEmployees = payrollList.Count;
            ViewBag.TotalBasicSalary = payrollList.Sum(p => p.BasicSalary);
            ViewBag.TotalAllowances = payrollList.Sum(p => p.TotalAllowances);
            ViewBag.TotalDeductions = payrollList.Sum(p => p.TotalDeductions);
            ViewBag.TotalGrossSalary = payrollList.Sum(p => p.GrossSalary);
            ViewBag.TotalNetSalary = payrollList.Sum(p => p.NetSalary);

            return View(payrollList);
        }

        #endregion

        #region Salary History

        // GET: Reports/SalaryHistory
        public async Task<IActionResult> SalaryHistory(int? employeeId, int? year)
        {
            var selectedYear = year ?? DateTime.Now.Year;
            ViewData["SelectedEmployee"] = employeeId;
            ViewData["SelectedYear"] = selectedYear;

            // Get employees for dropdown
            ViewBag.Employees = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .OrderBy(e => e.Name)
                .ToListAsync();

            if (!employeeId.HasValue)
            {
                return View(new List<Payroll>());
            }

            var employee = await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId.Value);

            if (employee == null)
            {
                return NotFound();
            }

            ViewBag.Employee = employee;

            var salaryHistory = await _context.Payrolls
                .Include(p => p.PayrollDetails)
                    .ThenInclude(pd => pd.AllowanceDeduction)
                .Where(p => p.EmployeeId == employeeId.Value && p.Year == selectedYear)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToListAsync();

            // Calculate summary
            ViewBag.TotalMonths = salaryHistory.Count;
            ViewBag.TotalEarnings = salaryHistory.Sum(p => p.GrossSalary);
            ViewBag.TotalDeductions = salaryHistory.Sum(p => p.TotalDeductions);
            ViewBag.TotalNetPay = salaryHistory.Sum(p => p.NetSalary);
            ViewBag.AverageNetPay = salaryHistory.Any() ? salaryHistory.Average(p => p.NetSalary) : 0;

            return View(salaryHistory);
        }

        #endregion
    }
}
