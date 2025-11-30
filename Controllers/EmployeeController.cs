using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
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
                            select e;

            // Search by name, email, or employee code
            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e => e.Name.Contains(searchString)
                                               || e.Email.Contains(searchString)
                                               || e.EmployeeCode.Contains(searchString));
            }

            // Filter by department
            if (!string.IsNullOrEmpty(department))
            {
                employees = employees.Where(e => e.Department == department);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<EmploymentStatus>(status, out var statusEnum))
            {
                employees = employees.Where(e => e.Status == statusEnum);
            }

            // Get unique departments for filter dropdown
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
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            // Generate new employee code
            var newEmployeeCode = GenerateEmployeeCode();
            ViewBag.GeneratedEmployeeCode = newEmployeeCode;

            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeCode,Name,Email,Phone,Department,Designation,BasicSalary,JoiningDate,Status,Address,City,PostalCode")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                // Check if employee code already exists
                if (await _context.Employees.AnyAsync(e => e.EmployeeCode == employee.EmployeeCode))
                {
                    ModelState.AddModelError("EmployeeCode", "Employee code already exists.");
                    ViewBag.GeneratedEmployeeCode = employee.EmployeeCode;
                    return View(employee);
                }

                // Check if email already exists
                if (await _context.Employees.AnyAsync(e => e.Email == employee.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    ViewBag.GeneratedEmployeeCode = employee.EmployeeCode;
                    return View(employee);
                }

                employee.CreatedAt = DateTime.Now;
                _context.Add(employee);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Employee {employee.Name} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.GeneratedEmployeeCode = employee.EmployeeCode;
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,EmployeeCode,Name,Email,Phone,Department,Designation,BasicSalary,JoiningDate,Status,Address,City,PostalCode,CreatedAt")] Employee employee)
        {
            if (id != employee.EmployeeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists for another employee
                    if (await _context.Employees.AnyAsync(e => e.Email == employee.Email && e.EmployeeId != employee.EmployeeId))
                    {
                        ModelState.AddModelError("Email", "Email already exists for another employee.");
                        return View(employee);
                    }

                    employee.UpdatedAt = DateTime.Now;
                    _context.Update(employee);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Employee {employee.Name} updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Employee {employee.Name} deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }

        // Generate unique employee code
        private string GenerateEmployeeCode()
        {
            var lastEmployee = _context.Employees
                .OrderByDescending(e => e.EmployeeId)
                .FirstOrDefault();

            if (lastEmployee == null)
            {
                return "EMP001";
            }

            // Extract number from last employee code
            var lastCode = lastEmployee.EmployeeCode;
            var numberPart = new string(lastCode.Where(char.IsDigit).ToArray());

            if (int.TryParse(numberPart, out int lastNumber))
            {
                var newNumber = lastNumber + 1;
                return $"EMP{newNumber:D3}";
            }

            return "EMP001";
        }
    }
}