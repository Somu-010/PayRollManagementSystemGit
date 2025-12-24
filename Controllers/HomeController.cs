using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
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
            // Get department statistics
            var totalDepartments = await _context.Departments.CountAsync();
            var activeDepartments = await _context.Departments
                .CountAsync(d => d.Status == DepartmentStatus.Active);
            var totalEmployeesInDepartments = await _context.Employees
                .Where(e => e.DepartmentId != null)
                .CountAsync();
            
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

            ViewBag.TotalDepartments = totalDepartments;
            ViewBag.ActiveDepartments = activeDepartments;
            ViewBag.TotalEmployeesInDepartments = totalEmployeesInDepartments;
            ViewBag.TopDepartments = topDepartments;

            ViewBag.TotalDesignations = totalDesignations;
            ViewBag.ActiveDesignations = activeDesignations;
            ViewBag.TopDesignations = topDesignations;

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
