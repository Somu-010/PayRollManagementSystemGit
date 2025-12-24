using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index(string searchString, string department, string status)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentDepartment"] = department;
            ViewData["CurrentStatus"] = status;

            var employees = from e in _context.Employees
                            .Include(e => e.DepartmentNavigation)
                            .Include(e => e.ShiftNavigation)
                            select e;

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e => e.Name.Contains(searchString)
                                               || e.Email.Contains(searchString)
                                               || e.EmployeeCode.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(department))
            {
                employees = employees.Where(e => e.Department == department);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<EmploymentStatus>(status, out var statusEnum))
            {
                employees = employees.Where(e => e.Status == statusEnum);
            }

            ViewBag.Departments = await _context.Employees
                .Select(e => e.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            return View(await employees.OrderByDescending(e => e.CreatedAt).ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.DepartmentNavigation)
                .Include(e => e.DesignationNavigation)
                .Include(e => e.ShiftNavigation)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Create
        public async Task<IActionResult> Create()
        {
            var newEmployeeCode = GenerateEmployeeCode();
            ViewBag.GeneratedEmployeeCode = newEmployeeCode;

            await LoadDropdownData();

            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeCode,Name,Email,Phone,DepartmentId,DesignationId,ShiftId,BasicSalary,JoiningDate,Status,Address,City,PostalCode")] Employee employee)
        {
            // Remove Department and Designation from ModelState validation since they'll be populated from the IDs
            ModelState.Remove("Department");
            ModelState.Remove("Designation");
            
            if (ModelState.IsValid)
            {
                // Check if employee code already exists
                if (await _context.Employees.AnyAsync(e => e.EmployeeCode == employee.EmployeeCode))
                {
                    ModelState.AddModelError("EmployeeCode", "Employee code already exists.");
                    await LoadDropdownData();
                    ViewBag.GeneratedEmployeeCode = employee.EmployeeCode;
                    return View(employee);
                }

                // Check if email already exists
                if (await _context.Employees.AnyAsync(e => e.Email == employee.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    await LoadDropdownData();
                    ViewBag.GeneratedEmployeeCode = employee.EmployeeCode;
                    return View(employee);
                }

                // Get Department name from selected DepartmentId
                if (employee.DepartmentId.HasValue)
                {
                    var department = await _context.Departments.FindAsync(employee.DepartmentId.Value);
                    employee.Department = department?.Name ?? string.Empty;
                }

                // Get Designation name from selected DesignationId
                if (employee.DesignationId.HasValue)
                {
                    var designation = await _context.Designations.FindAsync(employee.DesignationId.Value);
                    employee.Designation = designation?.Title ?? string.Empty;
                }

                employee.CreatedAt = DateTime.Now;
                _context.Add(employee);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Employee {employee.Name} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            await LoadDropdownData();
            ViewBag.GeneratedEmployeeCode = employee.EmployeeCode;
            return View(employee);
        }

        // AJAX: Get Employee data for Edit Modal
        [HttpGet]
        public async Task<IActionResult> GetEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            return Json(new
            {
                employeeId = employee.EmployeeId,
                employeeCode = employee.EmployeeCode,
                name = employee.Name,
                email = employee.Email,
                phone = employee.Phone,
                department = employee.Department,
                designation = employee.Designation,
                basicSalary = employee.BasicSalary,
                joiningDate = employee.JoiningDate.ToString("yyyy-MM-dd"),
                status = employee.Status.ToString(),
                address = employee.Address,
                city = employee.City,
                postalCode = employee.PostalCode
            });
        }

        // POST: AJAX Edit Employee (Inline Modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee employee)
        {
            try
            {
                var existingEmployee = await _context.Employees.FindAsync(employee.EmployeeId);
                if (existingEmployee == null)
                {
                    return Json(new { success = false, message = "Employee not found" });
                }

                if (await _context.Employees.AnyAsync(e => e.Email == employee.Email && e.EmployeeId != employee.EmployeeId))
                {
                    return Json(new { success = false, message = "Email already exists for another employee" });
                }

                existingEmployee.Name = employee.Name;
                existingEmployee.Email = employee.Email;
                existingEmployee.Phone = employee.Phone;
                existingEmployee.Department = employee.Department;
                existingEmployee.Designation = employee.Designation;
                existingEmployee.BasicSalary = employee.BasicSalary;
                existingEmployee.JoiningDate = employee.JoiningDate;
                existingEmployee.Status = employee.Status;
                existingEmployee.Address = employee.Address;
                existingEmployee.City = employee.City;
                existingEmployee.PostalCode = employee.PostalCode;
                existingEmployee.UpdatedAt = DateTime.Now;

                _context.Update(existingEmployee);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Employee {employee.Name} updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating employee: " + ex.Message });
            }
        }

        // POST: AJAX Delete Employee (Inline Modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return Json(new { success = false, message = "Employee not found" });
                }

                var employeeName = employee.Name;
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Employee {employeeName} deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting employee: " + ex.Message });
            }
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }

        private string GenerateEmployeeCode()
        {
            var lastEmployee = _context.Employees
                .OrderByDescending(e => e.EmployeeId)
                .FirstOrDefault();

            if (lastEmployee == null)
            {
                return "EMP001";
            }

            var lastCode = lastEmployee.EmployeeCode;
            var numberPart = new string(lastCode.Where(char.IsDigit).ToArray());

            if (int.TryParse(numberPart, out int lastNumber))
            {
                var newNumber = lastNumber + 1;
                return $"EMP{newNumber:D3}";
            }

            return "EMP001";
        }

        // Helper method to load dropdown data
        private async Task LoadDropdownData()
        {
            ViewBag.Departments = await _context.Departments
                .Where(d => d.Status == DepartmentStatus.Active)
                .OrderBy(d => d.Name)
                .Select(d => new { d.DepartmentId, d.Name })
                .ToListAsync();

            ViewBag.Designations = await _context.Designations
                .Where(d => d.Status == DesignationStatus.Active)
                .OrderBy(d => d.Title)
                .Select(d => new { d.DesignationId, d.Title, d.DepartmentId })
                .ToListAsync();

            ViewBag.Shifts = await _context.Shifts
                .Where(s => s.Status == ShiftStatus.Active)
                .OrderBy(s => s.ShiftName)
                .Select(s => new { s.ShiftId, s.ShiftName })
                .ToListAsync();
        }
    }
}
