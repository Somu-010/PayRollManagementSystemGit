using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class ComponentTemplateController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComponentTemplateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ComponentTemplate
        public async Task<IActionResult> Index(string searchString, string industryType, string employeeLevel)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentIndustry"] = industryType;
            ViewData["CurrentLevel"] = employeeLevel;

            var templates = from t in _context.ComponentTemplates
                           .Include(t => t.TemplateItems)
                            select t;

            if (!string.IsNullOrEmpty(searchString))
            {
                templates = templates.Where(t => t.Name.Contains(searchString)
                                              || t.Description.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(industryType) && Enum.TryParse<IndustryType>(industryType, out var industry))
            {
                templates = templates.Where(t => t.IndustryType == industry);
            }

            if (!string.IsNullOrEmpty(employeeLevel) && Enum.TryParse<EmployeeLevel>(employeeLevel, out var level))
            {
                templates = templates.Where(t => t.EmployeeLevel == level);
            }

            return View(await templates.OrderByDescending(t => t.CreatedAt).ToListAsync());
        }

        // GET: ComponentTemplate/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var template = await _context.ComponentTemplates
                .Include(t => t.TemplateItems)
                    .ThenInclude(ti => ti.Component)
                .FirstOrDefaultAsync(t => t.TemplateId == id);

            if (template == null) return NotFound();

            return View(template);
        }

        // GET: ComponentTemplate/Create
        public async Task<IActionResult> Create()
        {
            // Get all active components
            ViewBag.AllComponents = await _context.AllowanceDeductions
                .Where(c => c.Status == ComponentStatus.Active)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View();
        }

        // POST: ComponentTemplate/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ComponentTemplate template, List<int> selectedComponents, List<decimal?> customValues)
        {
            ModelState.Remove("TemplateItems");

            if (ModelState.IsValid)
            {
                // Check if template name exists
                if (await _context.ComponentTemplates.AnyAsync(t => t.Name == template.Name))
                {
                    ModelState.AddModelError("Name", "Template name already exists.");
                    ViewBag.AllComponents = await _context.AllowanceDeductions
                        .Where(c => c.Status == ComponentStatus.Active)
                        .OrderBy(c => c.Type)
                        .ThenBy(c => c.Name)
                        .ToListAsync();
                    return View(template);
                }

                if (selectedComponents == null || !selectedComponents.Any())
                {
                    ModelState.AddModelError("", "Please select at least one component for this template.");
                    ViewBag.AllComponents = await _context.AllowanceDeductions
                        .Where(c => c.Status == ComponentStatus.Active)
                        .OrderBy(c => c.Type)
                        .ThenBy(c => c.Name)
                        .ToListAsync();
                    return View(template);
                }

                template.CreatedAt = DateTime.Now;
                _context.Add(template);
                await _context.SaveChangesAsync();

                // Add template items
                for (int i = 0; i < selectedComponents.Count; i++)
                {
                    var item = new ComponentTemplateItem
                    {
                        TemplateId = template.TemplateId,
                        AllowanceDeductionId = selectedComponents[i],
                        CustomValue = customValues != null && i < customValues.Count ? customValues[i] : null,
                        DisplayOrder = i
                    };
                    _context.ComponentTemplateItems.Add(item);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Template '{template.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AllComponents = await _context.AllowanceDeductions
                .Where(c => c.Status == ComponentStatus.Active)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();
            return View(template);
        }

        // POST: ComponentTemplate/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.ComponentTemplates
                .Include(t => t.TemplateItems)
                .FirstOrDefaultAsync(t => t.TemplateId == id);

            if (template == null)
            {
                return Json(new { success = false, message = "Template not found." });
            }

            _context.ComponentTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Template '{template.Name}' deleted successfully!" });
        }

        // POST: ComponentTemplate/ApplyToEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyToEmployee(int templateId, int employeeId)
        {
            var template = await _context.ComponentTemplates
                .Include(t => t.TemplateItems)
                    .ThenInclude(ti => ti.Component)
                .FirstOrDefaultAsync(t => t.TemplateId == templateId);

            if (template == null)
            {
                return Json(new { success = false, message = "Template not found." });
            }

            // Increment usage count
            template.UsageCount++;
            template.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Template '{template.Name}' applied successfully!",
                componentCount = template.TemplateItems?.Count ?? 0
            });
        }

        // GET: ComponentTemplate/Clone/5
        public async Task<IActionResult> Clone(int? id)
        {
            if (id == null) return NotFound();

            var originalTemplate = await _context.ComponentTemplates
                .Include(t => t.TemplateItems)
                .FirstOrDefaultAsync(t => t.TemplateId == id);

            if (originalTemplate == null) return NotFound();

            // Create clone
            var clonedTemplate = new ComponentTemplate
            {
                Name = originalTemplate.Name + " (Copy)",
                Description = originalTemplate.Description,
                IndustryType = originalTemplate.IndustryType,
                EmployeeLevel = originalTemplate.EmployeeLevel,
                IsActive = false, // Start as inactive
                CreatedAt = DateTime.Now
            };

            _context.ComponentTemplates.Add(clonedTemplate);
            await _context.SaveChangesAsync();

            // Clone template items
            if (originalTemplate.TemplateItems != null)
            {
                foreach (var item in originalTemplate.TemplateItems)
                {
                    var clonedItem = new ComponentTemplateItem
                    {
                        TemplateId = clonedTemplate.TemplateId,
                        AllowanceDeductionId = item.AllowanceDeductionId,
                        CustomValue = item.CustomValue,
                        DisplayOrder = item.DisplayOrder
                    };
                    _context.ComponentTemplateItems.Add(clonedItem);
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Template cloned successfully! Edit the new template to customize it.";
            return RedirectToAction(nameof(Details), new { id = clonedTemplate.TemplateId });
        }
    }
}