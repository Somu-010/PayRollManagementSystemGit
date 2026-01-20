using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;
using System.Security.Claims;

namespace PayRollManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HolidayController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HolidayController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Holiday
        public async Task<IActionResult> Index(int? year)
        {
            var selectedYear = year ?? DateTime.Now.Year;
            ViewData["SelectedYear"] = selectedYear;

            var holidays = await _context.Holidays
                .Where(h => h.Date.Year == selectedYear)
                .OrderBy(h => h.Date)
                .ToListAsync();

            // Get weekend settings
            var weekendSetting = await _context.WeekendSettings
                .Where(w => w.IsActive)
                .OrderByDescending(w => w.EffectiveFrom)
                .FirstOrDefaultAsync();

            ViewBag.WeekendSetting = weekendSetting;

            return View(holidays);
        }

        // GET: Holiday/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Holiday/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Holiday holiday)
        {
            if (ModelState.IsValid)
            {
                holiday.CreatedAt = DateTime.Now;
                _context.Add(holiday);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Holiday created successfully!";
                return RedirectToAction(nameof(Index), new { year = holiday.Date.Year });
            }
            return View(holiday);
        }

        // GET: Holiday/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null)
            {
                return NotFound();
            }
            return View(holiday);
        }

        // POST: Holiday/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Holiday holiday)
        {
            if (id != holiday.HolidayId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    holiday.UpdatedAt = DateTime.Now;
                    _context.Update(holiday);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Holiday updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HolidayExists(holiday.HolidayId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { year = holiday.Date.Year });
            }
            return View(holiday);
        }

        // GET: Holiday/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var holiday = await _context.Holidays
                .FirstOrDefaultAsync(m => m.HolidayId == id);
            if (holiday == null)
            {
                return NotFound();
            }

            return View(holiday);
        }

        // POST: Holiday/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday != null)
            {
                _context.Holidays.Remove(holiday);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Holiday deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Holiday/WeekendSettings
        public async Task<IActionResult> WeekendSettings()
        {
            var currentSetting = await _context.WeekendSettings
                .Where(w => w.IsActive)
                .OrderByDescending(w => w.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (currentSetting == null)
            {
                // Create default setting (Friday-Saturday for Bangladesh)
                currentSetting = new WeekendSetting
                {
                    IsFridayWeekend = true,
                    IsSaturdayWeekend = false,
                    IsSundayWeekend = false,
                    EffectiveFrom = DateTime.Now,
                    IsActive = true
                };
            }

            var allSettings = await _context.WeekendSettings
                .OrderByDescending(w => w.EffectiveFrom)
                .ToListAsync();

            ViewBag.AllSettings = allSettings;

            return View(currentSetting);
        }

        // POST: Holiday/UpdateWeekendSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWeekendSettings(WeekendSetting model)
        {
            if (ModelState.IsValid)
            {
                // Deactivate all previous settings
                var previousSettings = await _context.WeekendSettings
                    .Where(w => w.IsActive)
                    .ToListAsync();

                foreach (var setting in previousSettings)
                {
                    setting.IsActive = false;
                    setting.UpdatedAt = DateTime.Now;
                    setting.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
                }

                // Create new setting
                var newSetting = new WeekendSetting
                {
                    IsFridayWeekend = model.IsFridayWeekend,
                    IsSaturdayWeekend = model.IsSaturdayWeekend,
                    IsSundayWeekend = model.IsSundayWeekend,
                    IsMondayWeekend = model.IsMondayWeekend,
                    IsTuesdayWeekend = model.IsTuesdayWeekend,
                    IsWednesdayWeekend = model.IsWednesdayWeekend,
                    IsThursdayWeekend = model.IsThursdayWeekend,
                    EffectiveFrom = model.EffectiveFrom,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System"
                };

                _context.WeekendSettings.Add(newSetting);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Weekend settings updated successfully!";
                return RedirectToAction(nameof(WeekendSettings));
            }

            return View("WeekendSettings", model);
        }

        // POST: Holiday/BulkImport - Import holidays for a year
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkImport(int year)
        {
            try
            {
                // Check if holidays already exist
                var existingCount = await _context.Holidays.CountAsync(h => h.Date.Year == year);
                if (existingCount > 0)
                {
                    TempData["Warning"] = $"Holidays already exist for {year}. Please delete them first if you want to reimport.";
                    return RedirectToAction(nameof(Index), new { year });
                }

                // Add default Bangladeshi holidays
                var holidays = GetDefaultBangladeshiHolidays(year);
                _context.Holidays.AddRange(holidays);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Successfully imported {holidays.Count} holidays for {year}!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error importing holidays: " + ex.Message;
            }

            return RedirectToAction(nameof(Index), new { year });
        }

        private bool HolidayExists(int id)
        {
            return _context.Holidays.Any(e => e.HolidayId == id);
        }

        private List<Holiday> GetDefaultBangladeshiHolidays(int year)
        {
            var holidays = new List<Holiday>
            {
                new Holiday { Name = "Shaheed Day & International Mother Language Day", Date = new DateTime(year, 2, 21), Type = HolidayType.National, IsActive = true },
                new Holiday { Name = "Birthday of Bangabandhu & National Children's Day", Date = new DateTime(year, 3, 17), Type = HolidayType.National, IsActive = true },
                new Holiday { Name = "Independence Day", Date = new DateTime(year, 3, 26), Type = HolidayType.National, IsActive = true },
                new Holiday { Name = "Bengali New Year", Date = new DateTime(year, 4, 14), Type = HolidayType.National, IsActive = true },
                new Holiday { Name = "May Day", Date = new DateTime(year, 5, 1), Type = HolidayType.National, IsActive = true },
                new Holiday { Name = "National Mourning Day", Date = new DateTime(year, 8, 15), Type = HolidayType.National, IsActive = true },
                new Holiday { Name = "Victory Day", Date = new DateTime(year, 12, 16), Type = HolidayType.National, IsActive = true },
                new Holiday { Name = "Christmas Day", Date = new DateTime(year, 12, 25), Type = HolidayType.Religious, IsActive = true }
            };

            return holidays;
        }
    }
}
