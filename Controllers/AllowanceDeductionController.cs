using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class AllowanceDeductionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AllowanceDeductionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AllowanceDeduction
        public async Task<IActionResult> Index(string searchString, string type, string status)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentType"] = type;
            ViewData["CurrentStatus"] = status;

            var components = from c in _context.AllowanceDeductions
                             select c;

            // Search by name or code
            if (!string.IsNullOrEmpty(searchString))
            {
                components = components.Where(c => c.Name.Contains(searchString)
                                               || c.Code.Contains(searchString));
            }

            // Filter by type
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ComponentType>(type, out var typeEnum))
            {
                components = components.Where(c => c.Type == typeEnum);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ComponentStatus>(status, out var statusEnum))
            {
                components = components.Where(c => c.Status == statusEnum);
            }

            return View(await components.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync());
        }

        // GET: AllowanceDeduction/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var component = await _context.AllowanceDeductions
                .FirstOrDefaultAsync(m => m.AllowanceDeductionId == id);

            if (component == null)
            {
                return NotFound();
            }

            return View(component);
        }

        // GET: AllowanceDeduction/Create
        public IActionResult Create()
        {
            var newCode = GenerateComponentCode();
            ViewBag.GeneratedCode = newCode;
            return View();
        }

        // POST: AllowanceDeduction/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Description,Type,CalculationMethod,Value,IsTaxable,IsMandatory,AppliesToAll,MinimumSalaryThreshold,MaximumCap,Status,DisplayOrder,EffectiveFrom,EffectiveUntil")] AllowanceDeduction component)
        {
            if (ModelState.IsValid)
            {
                // Check if code already exists
                if (await _context.AllowanceDeductions.AnyAsync(c => c.Code == component.Code))
                {
                    ModelState.AddModelError("Code", "Code already exists.");
                    ViewBag.GeneratedCode = component.Code;
                    return View(component);
                }

                // Validate effective dates
                if (component.EffectiveFrom.HasValue && component.EffectiveUntil.HasValue
                    && component.EffectiveUntil < component.EffectiveFrom)
                {
                    ModelState.AddModelError("EffectiveUntil", "End date must be after start date.");
                    ViewBag.GeneratedCode = component.Code;
                    return View(component);
                }

                // Validate threshold and cap
                if (component.MinimumSalaryThreshold.HasValue && component.MaximumCap.HasValue
                    && component.CalculationMethod == CalculationMethod.PercentageOfBasic)
                {
                    if (component.MaximumCap < 0)
                    {
                        ModelState.AddModelError("MaximumCap", "Maximum cap must be positive.");
                        ViewBag.GeneratedCode = component.Code;
                        return View(component);
                    }
                }

                component.CreatedAt = DateTime.Now;
                _context.Add(component);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"{component.TypeDisplay} '{component.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.GeneratedCode = component.Code;
            return View(component);
        }

        // GET: Get component data for editing (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetComponent(int id)
        {
            var component = await _context.AllowanceDeductions.FindAsync(id);
            if (component == null)
            {
                return NotFound();
            }

            return Json(new
            {
                allowanceDeductionId = component.AllowanceDeductionId,
                code = component.Code,
                name = component.Name,
                description = component.Description,
                type = component.Type.ToString(),
                calculationMethod = component.CalculationMethod.ToString(),
                value = component.Value,
                isTaxable = component.IsTaxable,
                isMandatory = component.IsMandatory,
                appliesToAll = component.AppliesToAll,
                minimumSalaryThreshold = component.MinimumSalaryThreshold,
                maximumCap = component.MaximumCap,
                status = component.Status.ToString(),
                displayOrder = component.DisplayOrder,
                effectiveFrom = component.EffectiveFrom?.ToString("yyyy-MM-dd"),
                effectiveUntil = component.EffectiveUntil?.ToString("yyyy-MM-dd")
            });
        }

        // POST: AllowanceDeduction/Edit (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] AllowanceDeduction component)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingComponent = await _context.AllowanceDeductions.FindAsync(component.AllowanceDeductionId);
                    if (existingComponent == null)
                    {
                        return Json(new { success = false, message = "Component not found." });
                    }

                    // Validate effective dates
                    if (component.EffectiveFrom.HasValue && component.EffectiveUntil.HasValue
                        && component.EffectiveUntil < component.EffectiveFrom)
                    {
                        return Json(new { success = false, message = "End date must be after start date." });
                    }

                    existingComponent.Name = component.Name;
                    existingComponent.Description = component.Description;
                    existingComponent.Type = component.Type;
                    existingComponent.CalculationMethod = component.CalculationMethod;
                    existingComponent.Value = component.Value;
                    existingComponent.IsTaxable = component.IsTaxable;
                    existingComponent.IsMandatory = component.IsMandatory;
                    existingComponent.AppliesToAll = component.AppliesToAll;
                    existingComponent.MinimumSalaryThreshold = component.MinimumSalaryThreshold;
                    existingComponent.MaximumCap = component.MaximumCap;
                    existingComponent.Status = component.Status;
                    existingComponent.DisplayOrder = component.DisplayOrder;
                    existingComponent.EffectiveFrom = component.EffectiveFrom;
                    existingComponent.EffectiveUntil = component.EffectiveUntil;
                    existingComponent.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = $"{component.TypeDisplay} '{component.Name}' updated successfully!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ComponentExists(component.AllowanceDeductionId))
                    {
                        return Json(new { success = false, message = "Component not found." });
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

        // POST: AllowanceDeduction/Delete (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var component = await _context.AllowanceDeductions.FindAsync(id);

            if (component == null)
            {
                return Json(new { success = false, message = "Component not found." });
            }

            _context.AllowanceDeductions.Remove(component);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"{component.TypeDisplay} '{component.Name}' deleted successfully!" });
        }

        // GET: Calculate preview (AJAX)
        [HttpGet]
        public IActionResult CalculatePreview(decimal basicSalary, string calculationMethod, decimal value)
        {
            if (!Enum.TryParse<CalculationMethod>(calculationMethod, out var method))
            {
                return Json(new { success = false, message = "Invalid calculation method" });
            }

            decimal calculatedAmount = 0;

            switch (method)
            {
                case CalculationMethod.FixedAmount:
                    calculatedAmount = value;
                    break;
                case CalculationMethod.PercentageOfBasic:
                    calculatedAmount = (basicSalary * value) / 100;
                    break;
                case CalculationMethod.PercentageOfGross:
                    // For preview, we'll use basic salary as approximation
                    calculatedAmount = (basicSalary * value) / 100;
                    break;
            }

            return Json(new
            {
                success = true,
                calculatedAmount = calculatedAmount,
                formattedAmount = calculatedAmount.ToString("N2")
            });
        }

        private bool ComponentExists(int id)
        {
            return _context.AllowanceDeductions.Any(e => e.AllowanceDeductionId == id);
        }

        // Generate unique component code
        private string GenerateComponentCode()
        {
            var lastComponent = _context.AllowanceDeductions
                .OrderByDescending(c => c.AllowanceDeductionId)
                .FirstOrDefault();

            if (lastComponent == null)
            {
                return "COMP001";
            }

            var lastCode = lastComponent.Code;
            var numberPart = new string(lastCode.Where(char.IsDigit).ToArray());

            if (int.TryParse(numberPart, out int lastNumber))
            {
                var newNumber = lastNumber + 1;
                return $"COMP{newNumber:D3}";
            }

            return "COMP001";
        }
    }
}