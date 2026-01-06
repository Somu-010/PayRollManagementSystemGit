using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CompanySettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CompanySettingsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: CompanySettings
        public async Task<IActionResult> Index()
        {
            var companySetting = await _context.CompanySettings.FirstOrDefaultAsync();

            if (companySetting == null)
            {
                // Create default company setting if none exists
                companySetting = new CompanySetting
                {
                    CompanyName = "Firotech",
                    Address = "Uttara Eastern City, Forid Market, Udayan School Road, Behind Sector 4, Road 18 Rail Line, Uttara, Dhaka 1230, Bangladesh",
                    Phone = "+880 1XXX-XXXXXX",
                    Email = "info@firotechbd.com",
                    Website = "http://firotechbd.com",
                    Currency = "BDT",
                    CurrencySymbol = "?",
                    FiscalYearStartMonth = 7,
                    Timezone = "Asia/Dhaka"
                };

                _context.CompanySettings.Add(companySetting);
                await _context.SaveChangesAsync();
            }

            return View(companySetting);
        }

        // POST: CompanySettings/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CompanySetting model, IFormFile? logoFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var companySetting = await _context.CompanySettings.FirstOrDefaultAsync();

                    if (companySetting == null)
                    {
                        return NotFound();
                    }

                    // Handle logo upload
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "company");
                        
                        // Create directory if it doesn't exist
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Delete old logo if exists
                        if (!string.IsNullOrEmpty(companySetting.LogoPath))
                        {
                            var oldLogoPath = Path.Combine(_environment.WebRootPath, companySetting.LogoPath.TrimStart('/'));
                            if (System.IO.File.Exists(oldLogoPath))
                            {
                                System.IO.File.Delete(oldLogoPath);
                            }
                        }

                        // Save new logo
                        var uniqueFileName = $"logo_{Guid.NewGuid()}{Path.GetExtension(logoFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await logoFile.CopyToAsync(fileStream);
                        }

                        companySetting.LogoPath = $"/uploads/company/{uniqueFileName}";
                    }

                    // Update properties
                    companySetting.CompanyName = model.CompanyName;
                    companySetting.Address = model.Address;
                    companySetting.Phone = model.Phone;
                    companySetting.Email = model.Email;
                    companySetting.Website = model.Website;
                    companySetting.TaxNumber = model.TaxNumber;
                    companySetting.Currency = model.Currency;
                    companySetting.CurrencySymbol = model.CurrencySymbol;
                    companySetting.FiscalYearStartMonth = model.FiscalYearStartMonth;
                    companySetting.Timezone = model.Timezone;
                    companySetting.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Company settings updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error updating company settings: {ex.Message}";
                }
            }

            return View("Index", model);
        }

        // POST: CompanySettings/RemoveLogo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogo()
        {
            var companySetting = await _context.CompanySettings.FirstOrDefaultAsync();

            if (companySetting != null && !string.IsNullOrEmpty(companySetting.LogoPath))
            {
                // Delete logo file
                var logoPath = Path.Combine(_environment.WebRootPath, companySetting.LogoPath.TrimStart('/'));
                if (System.IO.File.Exists(logoPath))
                {
                    System.IO.File.Delete(logoPath);
                }

                companySetting.LogoPath = null;
                companySetting.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Company logo removed successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
