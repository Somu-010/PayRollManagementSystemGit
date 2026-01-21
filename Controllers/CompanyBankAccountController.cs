using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class CompanyBankAccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyBankAccountController> _logger;

        public CompanyBankAccountController(ApplicationDbContext context, ILogger<CompanyBankAccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: CompanyBankAccount
        public async Task<IActionResult> Index()
        {
            var accounts = await _context.CompanyBankAccounts
                .OrderByDescending(c => c.IsPrimary)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(accounts);
        }

        // GET: CompanyBankAccount/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.CompanyBankAccounts
                .Include(c => c.PaymentTransactions)
                    .ThenInclude(p => p.Payroll)
                        .ThenInclude(pr => pr.Employee)
                .FirstOrDefaultAsync(m => m.CompanyBankAccountId == id);

            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // GET: CompanyBankAccount/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CompanyBankAccount/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AccountName,BankName,BranchName,AccountNumber,RoutingNumber,AccountType,SwiftCode,IsPrimary,Status,AvailableBalance,Description")] CompanyBankAccount account)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // If this is set as primary, set other accounts to non-primary
                    if (account.IsPrimary)
                    {
                        var existingAccounts = await _context.CompanyBankAccounts
                            .Where(c => c.IsPrimary)
                            .ToListAsync();

                        foreach (var existing in existingAccounts)
                        {
                            existing.IsPrimary = false;
                        }
                    }

                    account.CreatedAt = DateTime.Now;

                    _context.Add(account);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Company bank account created: {account.AccountName}");

                    TempData["Success"] = "Company bank account created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error creating company bank account: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while creating the account. Please try again.");
                }
            }

            return View(account);
        }

        // GET: CompanyBankAccount/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.CompanyBankAccounts.FindAsync(id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // POST: CompanyBankAccount/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CompanyBankAccountId,AccountName,BankName,BranchName,AccountNumber,RoutingNumber,AccountType,SwiftCode,IsPrimary,Status,AvailableBalance,Description,CreatedAt")] CompanyBankAccount account)
        {
            if (id != account.CompanyBankAccountId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // If this is set as primary, set other accounts to non-primary
                    if (account.IsPrimary)
                    {
                        var existingAccounts = await _context.CompanyBankAccounts
                            .Where(c => c.IsPrimary && c.CompanyBankAccountId != id)
                            .ToListAsync();

                        foreach (var existing in existingAccounts)
                        {
                            existing.IsPrimary = false;
                        }
                    }

                    account.UpdatedAt = DateTime.Now;

                    _context.Update(account);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Company bank account updated: {account.AccountName}");

                    TempData["Success"] = "Company bank account updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompanyBankAccountExists(account.CompanyBankAccountId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating company bank account: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while updating the account. Please try again.");
                }
            }

            return View(account);
        }

        // GET: CompanyBankAccount/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.CompanyBankAccounts
                .FirstOrDefaultAsync(m => m.CompanyBankAccountId == id);

            if (account == null)
            {
                return NotFound();
            }

            // Check if account has transactions
            var hasTransactions = await _context.PaymentTransactions
                .AnyAsync(p => p.CompanyBankAccountId == id);

            ViewBag.HasTransactions = hasTransactions;

            return View(account);
        }

        // POST: CompanyBankAccount/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Check if account has transactions
                var hasTransactions = await _context.PaymentTransactions
                    .AnyAsync(p => p.CompanyBankAccountId == id);

                if (hasTransactions)
                {
                    TempData["Error"] = "Cannot delete account that has payment transactions!";
                    return RedirectToAction(nameof(Index));
                }

                var account = await _context.CompanyBankAccounts.FindAsync(id);
                if (account != null)
                {
                    _context.CompanyBankAccounts.Remove(account);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Company bank account deleted: {account.AccountName}");

                    TempData["Success"] = "Company bank account deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting company bank account: {ex.Message}");
                TempData["Error"] = "An error occurred while deleting the account.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CompanyBankAccountExists(int id)
        {
            return _context.CompanyBankAccounts.Any(e => e.CompanyBankAccountId == id);
        }
    }
}
