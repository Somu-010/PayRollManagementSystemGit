using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,HR")]  // Only Admin and HR can access admin dashboard
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get employee statistics
            var totalEmployees = await _context.Employees.CountAsync();
            var activeEmployees = await _context.Employees
                .CountAsync(e => e.Status == EmploymentStatus.Active);
            
            // Calculate monthly payroll (sum of all active employee basic salaries)
            var monthlyPayroll = await _context.Employees
                .Where(e => e.Status == EmploymentStatus.Active)
                .SumAsync(e => e.BasicSalary);
            
            // Get employee count from last month for comparison
            var lastMonth = DateTime.Now.AddMonths(-1);
            var lastMonthEmployees = await _context.Employees
                .CountAsync(e => e.CreatedAt <= lastMonth);
            
            // Calculate percentage change
            var employeeGrowth = lastMonthEmployees > 0 
                ? ((totalEmployees - lastMonthEmployees) / (double)lastMonthEmployees * 100) 
                : 0;

            // Get recent employees (last 5)
            var recentEmployees = await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .Select(e => new {
                    e.EmployeeId,
                    e.Name,
                    e.EmployeeCode,
                    e.Email,
                    DepartmentName = e.Department ?? "N/A",
                    DesignationName = e.Designation ?? "N/A",
                    e.BasicSalary,
                    e.Status,
                    e.CreatedAt
                })
                .ToListAsync();

            // Get department statistics
            var totalDepartments = await _context.Departments.CountAsync();
            var activeDepartments = await _context.Departments
                .CountAsync(d => d.Status == DepartmentStatus.Active);
            
            // Get top departments by employee count
            var topDepartments = await _context.Departments
                .Include(d => d.Employees)
                .OrderByDescending(d => d.Employees!.Count)
                .Take(5)
                .Select(d => new {
                    d.DepartmentId,
                    d.Name,
                    d.DepartmentCode,
                    EmployeeCount = d.Employees!.Count
                })
                .ToListAsync();

            // Get designation statistics
            var totalDesignations = await _context.Designations.CountAsync();
            var activeDesignations = await _context.Designations
                .CountAsync(d => d.Status == DesignationStatus.Active);
            
            // Get top designations by employee count
            var topDesignations = await _context.Designations
                .Include(d => d.Department)
                .Include(d => d.Employees)
                .OrderByDescending(d => d.Employees!.Count)
                .Take(5)
                .Select(d => new {
                    d.DesignationId,
                    d.Title,
                    d.DesignationCode,
                    DepartmentName = d.Department!.Name,
                    EmployeeCount = d.Employees!.Count,
                    d.MinimumSalary,
                    d.MaximumSalary
                })
                .ToListAsync();

            // Get shift statistics
            var totalShifts = await _context.Shifts.CountAsync();
            var activeShifts = await _context.Shifts
                .CountAsync(s => s.Status == ShiftStatus.Active);
            
            // Get top shifts by assigned employees
            var topShifts = await _context.Shifts
                .Where(s => s.Status == ShiftStatus.Active)
                .OrderByDescending(s => s.AssignedEmployees)
                .Take(5)
                .Select(s => new {
                    s.ShiftId,
                    s.ShiftName,
                    s.ShiftCode,
                    s.StartTime,
                    s.EndTime,
                    s.AssignedEmployees,
                    s.IsNightShift
                })
                .ToListAsync();

            // Get attendance statistics for today
            var today = DateTime.Today;
            var todayAttendance = await _context.Attendances
                .Where(a => a.Date == today)
                .ToListAsync();

            var presentToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Present);
            var absentToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Absent);
            var lateToday = todayAttendance.Count(a => a.IsLate);
            var onLeaveToday = todayAttendance.Count(a => a.Status == AttendanceStatus.OnLeave);

            // Calculate attendance percentage
            var attendancePercentage = activeEmployees > 0 
                ? (presentToday / (double)activeEmployees * 100) 
                : 0;

            // Get recent attendance records (last 5)
            var recentAttendance = await _context.Attendances
                .Include(a => a.Employee)
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new {
                    a.AttendanceId,
                    EmployeeName = a.Employee!.Name,
                    a.Date,
                    a.CheckInTime,
                    a.CheckOutTime,
                    a.Status,
                    a.IsLate,
                    a.LateByMinutes,
                    a.TotalHours
                })
                .ToListAsync();

            // Employee statistics
            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.MonthlyPayroll = monthlyPayroll;
            ViewBag.EmployeeGrowth = employeeGrowth;
            ViewBag.RecentEmployees = recentEmployees;

            // Department statistics
            ViewBag.TotalDepartments = totalDepartments;
            ViewBag.ActiveDepartments = activeDepartments;
            ViewBag.TopDepartments = topDepartments;

            // Designation statistics
            ViewBag.TotalDesignations = totalDesignations;
            ViewBag.ActiveDesignations = activeDesignations;
            ViewBag.TopDesignations = topDesignations;

            // Shift statistics
            ViewBag.TotalShifts = totalShifts;
            ViewBag.ActiveShifts = activeShifts;
            ViewBag.TopShifts = topShifts;

            // Attendance statistics
            ViewBag.TodayAttendanceCount = todayAttendance.Count;
            ViewBag.PresentToday = presentToday;
            ViewBag.AbsentToday = absentToday;
            ViewBag.LateToday = lateToday;
            ViewBag.OnLeaveToday = onLeaveToday;
            ViewBag.AttendancePercentage = attendancePercentage;
            ViewBag.RecentAttendance = recentAttendance;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
