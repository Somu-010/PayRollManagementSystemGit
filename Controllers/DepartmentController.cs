using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Department
        public async Task<IActionResult> Index(string searchString, string status)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentStatus"] = status;

            var departments = from d in _context.Departments
                             .Include(d => d.Employees)
                              select d;

            // Search by name, code, or head
            if (!string.IsNullOrEmpty(searchString))
            {
                departments = departments.Where(d => d.Name.Contains(searchString)
                                               || d.DepartmentCode.Contains(searchString)
                                               || d.HeadOfDepartment.Contains(searchString));
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DepartmentStatus>(status, out var statusEnum))
            {
                departments = departments.Where(d => d.Status == statusEnum);
            }

            // Update employee counts
            var departmentList = await departments.OrderByDescending(d => d.CreatedAt).ToListAsync();
            foreach (var dept in departmentList)
            {
                dept.EmployeeCount = dept.Employees?.Count ?? 0;
            }

            return View(departmentList);
        }

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(m => m.DepartmentId == id);

            if (department == null)
            {
                return NotFound();
            }

            department.EmployeeCount = department.Employees?.Count ?? 0;
            return View(department);
        }

        // GET: Departments/Create
        public IActionResult Create()
        {
            var newDepartmentCode = GenerateDepartmentCode();
            ViewBag.GeneratedDepartmentCode = newDepartmentCode;
            return View();
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmentCode,Name,Description,HeadOfDepartment,ContactNumber,Email,Status,EstablishedDate")] Department department)
        {
            if (ModelState.IsValid)
            {
                // Check if department code already exists
                if (await _context.Departments.AnyAsync(d => d.DepartmentCode == department.DepartmentCode))
                {
                    ModelState.AddModelError("DepartmentCode", "Department code already exists.");
                    ViewBag.GeneratedDepartmentCode = department.DepartmentCode;
                    return View(department);
                }

                // Check if department name already exists
                if (await _context.Departments.AnyAsync(d => d.Name == department.Name))
                {
                    ModelState.AddModelError("Name", "Department name already exists.");
                    ViewBag.GeneratedDepartmentCode = department.DepartmentCode;
                    return View(department);
                }

                department.CreatedAt = DateTime.Now;
                department.EmployeeCount = 0;
                _context.Add(department);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Department {department.Name} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.GeneratedDepartmentCode = department.DepartmentCode;
            return View(department);
        }

        // GET: Get department data for editing (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDepartment(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            return Json(new
            {
                departmentId = department.DepartmentId,
                departmentCode = department.DepartmentCode,
                name = department.Name,
                description = department.Description,
                headOfDepartment = department.HeadOfDepartment,
                contactNumber = department.ContactNumber,
                email = department.Email,
                status = department.Status.ToString(),
                establishedDate = department.EstablishedDate?.ToString("yyyy-MM-dd")
            });
        }

        // POST: Departments/Edit (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] Department department)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if name already exists for another department
                    if (await _context.Departments.AnyAsync(d => d.Name == department.Name && d.DepartmentId != department.DepartmentId))
                    {
                        return Json(new { success = false, message = "Department name already exists for another department." });
                    }

                    var existingDept = await _context.Departments.FindAsync(department.DepartmentId);
                    if (existingDept == null)
                    {
                        return Json(new { success = false, message = "Department not found." });
                    }

                    existingDept.Name = department.Name;
                    existingDept.Description = department.Description;
                    existingDept.HeadOfDepartment = department.HeadOfDepartment;
                    existingDept.ContactNumber = department.ContactNumber;
                    existingDept.Email = department.Email;
                    existingDept.Status = department.Status;
                    existingDept.EstablishedDate = department.EstablishedDate;
                    existingDept.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = $"Department {department.Name} updated successfully!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DepartmentId))
                    {
                        return Json(new { success = false, message = "Department not found." });
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

        // POST: Departments/Delete (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
            {
                return Json(new { success = false, message = "Department not found." });
            }

            // Check if department has employees
            if (department.Employees != null && department.Employees.Any())
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot delete department {department.Name} because it has {department.Employees.Count} employee(s). Please reassign or remove employees first."
                });
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Department {department.Name} deleted successfully!" });
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.DepartmentId == id);
        }

        // Generate unique department code
        private string GenerateDepartmentCode()
        {
            var lastDepartment = _context.Departments
                .OrderByDescending(d => d.DepartmentId)
                .FirstOrDefault();

            if (lastDepartment == null)
            {
                return "DEPT001";
            }

            var lastCode = lastDepartment.DepartmentCode;
            var numberPart = new string(lastCode.Where(char.IsDigit).ToArray());

            if (int.TryParse(numberPart, out int lastNumber))
            {
                var newNumber = lastNumber + 1;
                return $"DEPT{newNumber:D3}";
            }

            return "DEPT001";
        }
    }
}