using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class DesignationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DesignationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Designation
        public async Task<IActionResult> Index(string searchString, string department, string status)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentDepartment"] = department;
            ViewData["CurrentStatus"] = status;

            var designations = from d in _context.Designations
                               .Include(d => d.Department)
                               .Include(d => d.Employees)
                               select d;

            // Search by title, code, or department
            if (!string.IsNullOrEmpty(searchString))
            {
                designations = designations.Where(d => d.Title.Contains(searchString)
                                               || d.DesignationCode.Contains(searchString)
                                               || (d.Department != null && d.Department.Name.Contains(searchString)));
            }

            // Filter by department
            if (!string.IsNullOrEmpty(department) && int.TryParse(department, out var deptId))
            {
                designations = designations.Where(d => d.DepartmentId == deptId);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DesignationStatus>(status, out var statusEnum))
            {
                designations = designations.Where(d => d.Status == statusEnum);
            }

            // Update employee counts
            var designationList = await designations.OrderByDescending(d => d.CreatedAt).ToListAsync();
            foreach (var desig in designationList)
            {
                desig.EmployeeCount = desig.Employees?.Count ?? 0;
            }

            // Get departments for filter dropdown
            ViewBag.Departments = await _context.Departments
                .Where(d => d.Status == DepartmentStatus.Active)
                .OrderBy(d => d.Name)
                .ToListAsync();

            return View(designationList);
        }

        // GET: Designation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var designation = await _context.Designations
                .Include(d => d.Department)
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(m => m.DesignationId == id);

            if (designation == null)
            {
                return NotFound();
            }

            designation.EmployeeCount = designation.Employees?.Count ?? 0;
            return View(designation);
        }

        // GET: Designation/Create
        public async Task<IActionResult> Create()
        {
            var newDesignationCode = GenerateDesignationCode();
            ViewBag.GeneratedDesignationCode = newDesignationCode;
            ViewBag.Departments = await _context.Departments
                .Where(d => d.Status == DepartmentStatus.Active)
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View();
        }

        // POST: Designation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DesignationCode,Title,Description,DepartmentId,Level,MinimumSalary,MaximumSalary,Status")] Designation designation)
        {
            if (ModelState.IsValid)
            {
                // Check if designation code already exists
                if (await _context.Designations.AnyAsync(d => d.DesignationCode == designation.DesignationCode))
                {
                    ModelState.AddModelError("DesignationCode", "Designation code already exists.");
                    ViewBag.GeneratedDesignationCode = designation.DesignationCode;
                    ViewBag.Departments = await _context.Departments
                        .Where(d => d.Status == DepartmentStatus.Active)
                        .OrderBy(d => d.Name)
                        .ToListAsync();
                    return View(designation);
                }

                // Validate salary range
                if (designation.MinimumSalary.HasValue && designation.MaximumSalary.HasValue
                    && designation.MinimumSalary > designation.MaximumSalary)
                {
                    ModelState.AddModelError("MaximumSalary", "Maximum salary must be greater than minimum salary.");
                    ViewBag.GeneratedDesignationCode = designation.DesignationCode;
                    ViewBag.Departments = await _context.Departments
                        .Where(d => d.Status == DepartmentStatus.Active)
                        .OrderBy(d => d.Name)
                        .ToListAsync();
                    return View(designation);
                }

                designation.CreatedAt = DateTime.Now;
                designation.EmployeeCount = 0;
                _context.Add(designation);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Designation {designation.Title} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.GeneratedDesignationCode = designation.DesignationCode;
            ViewBag.Departments = await _context.Departments
                .Where(d => d.Status == DepartmentStatus.Active)
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(designation);
        }

        // GET: Get designation data for editing (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDesignation(int id)
        {
            var designation = await _context.Designations
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.DesignationId == id);

            if (designation == null)
            {
                return NotFound();
            }

            return Json(new
            {
                designationId = designation.DesignationId,
                designationCode = designation.DesignationCode,
                title = designation.Title,
                description = designation.Description,
                departmentId = designation.DepartmentId,
                departmentName = designation.Department?.Name,
                level = designation.Level,
                minimumSalary = designation.MinimumSalary,
                maximumSalary = designation.MaximumSalary,
                status = designation.Status.ToString()
            });
        }

        // POST: Designation/Edit (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] Designation designation)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingDesig = await _context.Designations.FindAsync(designation.DesignationId);
                    if (existingDesig == null)
                    {
                        return Json(new { success = false, message = "Designation not found." });
                    }

                    // Validate salary range
                    if (designation.MinimumSalary.HasValue && designation.MaximumSalary.HasValue
                        && designation.MinimumSalary > designation.MaximumSalary)
                    {
                        return Json(new { success = false, message = "Maximum salary must be greater than minimum salary." });
                    }

                    existingDesig.Title = designation.Title;
                    existingDesig.Description = designation.Description;
                    existingDesig.DepartmentId = designation.DepartmentId;
                    existingDesig.Level = designation.Level;
                    existingDesig.MinimumSalary = designation.MinimumSalary;
                    existingDesig.MaximumSalary = designation.MaximumSalary;
                    existingDesig.Status = designation.Status;
                    existingDesig.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = $"Designation {designation.Title} updated successfully!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DesignationExists(designation.DesignationId))
                    {
                        return Json(new { success = false, message = "Designation not found." });
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

        // POST: Designation/Delete (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var designation = await _context.Designations
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.DesignationId == id);

            if (designation == null)
            {
                return Json(new { success = false, message = "Designation not found." });
            }

            // Check if designation has employees
            if (designation.Employees != null && designation.Employees.Any())
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot delete designation {designation.Title} because it has {designation.Employees.Count} employee(s). Please reassign or remove employees first."
                });
            }

            _context.Designations.Remove(designation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Designation {designation.Title} deleted successfully!" });
        }

        private bool DesignationExists(int id)
        {
            return _context.Designations.Any(e => e.DesignationId == id);
        }

        // Generate unique designation code
        private string GenerateDesignationCode()
        {
            var lastDesignation = _context.Designations
                .OrderByDescending(d => d.DesignationId)
                .FirstOrDefault();

            if (lastDesignation == null)
            {
                return "DES001";
            }

            var lastCode = lastDesignation.DesignationCode;
            var numberPart = new string(lastCode.Where(char.IsDigit).ToArray());

            if (int.TryParse(numberPart, out int lastNumber))
            {
                var newNumber = lastNumber + 1;
                return $"DES{newNumber:D3}";
            }

            return "DES001";
        }
    }
}