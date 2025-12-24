using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class ShiftController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShiftController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Shift
        public async Task<IActionResult> Index(string searchString, string status)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentStatus"] = status;

            var shifts = from s in _context.Shifts
                         .Include(s => s.Employees)
                         select s;

            // Search by name or code
            if (!string.IsNullOrEmpty(searchString))
            {
                shifts = shifts.Where(s => s.ShiftName.Contains(searchString)
                                       || s.ShiftCode.Contains(searchString));
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ShiftStatus>(status, out var statusEnum))
            {
                shifts = shifts.Where(s => s.Status == statusEnum);
            }

            // Update assigned employee counts
            var shiftList = await shifts.OrderByDescending(s => s.CreatedAt).ToListAsync();
            foreach (var shift in shiftList)
            {
                shift.AssignedEmployees = shift.Employees?.Count ?? 0;
            }

            return View(shiftList);
        }

        // GET: Shift/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.Shifts
                .Include(s => s.Employees)
                .FirstOrDefaultAsync(m => m.ShiftId == id);

            if (shift == null)
            {
                return NotFound();
            }

            shift.AssignedEmployees = shift.Employees?.Count ?? 0;
            return View(shift);
        }

        // GET: Shift/Create
        public IActionResult Create()
        {
            var newShiftCode = GenerateShiftCode();
            ViewBag.GeneratedShiftCode = newShiftCode;
            return View();
        }

        // POST: Shift/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShiftCode,ShiftName,Description,StartTime,EndTime,BreakDuration,GracePeriod,LateMarkAfter,HalfDayHours,FullDayHours,Status,IsNightShift,IsWeekendShift")] Shift shift)
        {
            if (ModelState.IsValid)
            {
                // Check if shift code already exists
                if (await _context.Shifts.AnyAsync(s => s.ShiftCode == shift.ShiftCode))
                {
                    ModelState.AddModelError("ShiftCode", "Shift code already exists.");
                    ViewBag.GeneratedShiftCode = shift.ShiftCode;
                    return View(shift);
                }

                // Check if shift name already exists
                if (await _context.Shifts.AnyAsync(s => s.ShiftName == shift.ShiftName))
                {
                    ModelState.AddModelError("ShiftName", "Shift name already exists.");
                    ViewBag.GeneratedShiftCode = shift.ShiftCode;
                    return View(shift);
                }

                // Validate time logic
                if (shift.EndTime <= shift.StartTime && !shift.IsNightShift)
                {
                    ModelState.AddModelError("EndTime", "End time must be after start time for day shifts. Enable 'Night Shift' for overnight shifts.");
                    ViewBag.GeneratedShiftCode = shift.ShiftCode;
                    return View(shift);
                }

                shift.CreatedAt = DateTime.Now;
                shift.AssignedEmployees = 0;
                _context.Add(shift);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Shift {shift.ShiftName} created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.GeneratedShiftCode = shift.ShiftCode;
            return View(shift);
        }

        // GET: Get shift data for editing (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetShift(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
            {
                return NotFound();
            }

            return Json(new
            {
                shiftId = shift.ShiftId,
                shiftCode = shift.ShiftCode,
                shiftName = shift.ShiftName,
                description = shift.Description,
                startTime = shift.StartTime.ToString(@"hh\:mm"),
                endTime = shift.EndTime.ToString(@"hh\:mm"),
                breakDuration = shift.BreakDuration,
                gracePeriod = shift.GracePeriod,
                lateMarkAfter = shift.LateMarkAfter,
                halfDayHours = shift.HalfDayHours,
                fullDayHours = shift.FullDayHours,
                status = shift.Status.ToString(),
                isNightShift = shift.IsNightShift,
                isWeekendShift = shift.IsWeekendShift
            });
        }

        // POST: Shift/Edit (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] Shift shift)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if name already exists for another shift
                    if (await _context.Shifts.AnyAsync(s => s.ShiftName == shift.ShiftName && s.ShiftId != shift.ShiftId))
                    {
                        return Json(new { success = false, message = "Shift name already exists for another shift." });
                    }

                    var existingShift = await _context.Shifts.FindAsync(shift.ShiftId);
                    if (existingShift == null)
                    {
                        return Json(new { success = false, message = "Shift not found." });
                    }

                    // Validate time logic
                    if (shift.EndTime <= shift.StartTime && !shift.IsNightShift)
                    {
                        return Json(new { success = false, message = "End time must be after start time for day shifts." });
                    }

                    existingShift.ShiftName = shift.ShiftName;
                    existingShift.Description = shift.Description;
                    existingShift.StartTime = shift.StartTime;
                    existingShift.EndTime = shift.EndTime;
                    existingShift.BreakDuration = shift.BreakDuration;
                    existingShift.GracePeriod = shift.GracePeriod;
                    existingShift.LateMarkAfter = shift.LateMarkAfter;
                    existingShift.HalfDayHours = shift.HalfDayHours;
                    existingShift.FullDayHours = shift.FullDayHours;
                    existingShift.Status = shift.Status;
                    existingShift.IsNightShift = shift.IsNightShift;
                    existingShift.IsWeekendShift = shift.IsWeekendShift;
                    existingShift.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = $"Shift {shift.ShiftName} updated successfully!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShiftExists(shift.ShiftId))
                    {
                        return Json(new { success = false, message = "Shift not found." });
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

        // POST: Shift/Delete (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var shift = await _context.Shifts
                .Include(s => s.Employees)
                .FirstOrDefaultAsync(s => s.ShiftId == id);

            if (shift == null)
            {
                return Json(new { success = false, message = "Shift not found." });
            }

            // Check if shift has employees
            if (shift.Employees != null && shift.Employees.Any())
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot delete shift {shift.ShiftName} because it has {shift.Employees.Count} employee(s). Please reassign or remove employees first."
                });
            }

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Shift {shift.ShiftName} deleted successfully!" });
        }

        private bool ShiftExists(int id)
        {
            return _context.Shifts.Any(e => e.ShiftId == id);
        }

        // Generate unique shift code
        private string GenerateShiftCode()
        {
            var lastShift = _context.Shifts
                .OrderByDescending(s => s.ShiftId)
                .FirstOrDefault();

            if (lastShift == null)
            {
                return "SH001";
            }

            var lastCode = lastShift.ShiftCode;
            var numberPart = new string(lastCode.Where(char.IsDigit).ToArray());

            if (int.TryParse(numberPart, out int lastNumber))
            {
                var newNumber = lastNumber + 1;
                return $"SH{newNumber:D3}";
            }

            return "SH001";
        }
    }
}