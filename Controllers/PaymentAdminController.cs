using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PaymentAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentAdminController> _logger;

        public PaymentAdminController(ApplicationDbContext context, ILogger<PaymentAdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: PaymentAdmin/StuckPayments
        public async Task<IActionResult> StuckPayments()
        {
            var stuckPayments = await _context.PaymentTransactions
                .Include(p => p.Payroll)
                    .ThenInclude(pr => pr.Employee)
                .Include(p => p.CompanyBankAccount)
                .Where(p => p.PaymentStatus == PaymentStatus.Processing ||
                           p.PaymentStatus == PaymentStatus.Pending)
                .Where(p => p.PaymentMethod == PaymentMethod.SSLCommerz)
                .OrderByDescending(p => p.InitiatedDate)
                .ToListAsync();

            return View(stuckPayments);
        }

        // POST: PaymentAdmin/FixStuckPayment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FixStuckPayment(int id)
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .Include(p => p.Payroll)
                    .FirstOrDefaultAsync(p => p.PaymentTransactionId == id);

                if (transaction == null)
                {
                    return Json(new { success = false, message = "Transaction not found." });
                }

                if (transaction.PaymentStatus != PaymentStatus.Processing)
                {
                    return Json(new { success = false, message = "Only processing payments can be fixed." });
                }

                // Update transaction to completed
                transaction.PaymentStatus = PaymentStatus.Completed;
                transaction.CompletedDate = DateTime.Now;
                transaction.ProcessedBy = User.Identity?.Name ?? "Admin";
                transaction.ProcessedDate = DateTime.Now;
                transaction.BankTransactionId = $"ADMIN-FIX-{transaction.PaymentTransactionId}";

                // Update payroll to paid
                if (transaction.Payroll != null)
                {
                    transaction.Payroll.Status = PayrollStatus.Paid;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Payment {transaction.TransactionNumber} fixed by {User.Identity?.Name}");

                return Json(new { success = true, message = "Payment fixed successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fixing payment: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: PaymentAdmin/FixAllStuckPayments
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FixAllStuckPayments()
        {
            try
            {
                var stuckPayments = await _context.PaymentTransactions
                    .Include(p => p.Payroll)
                    .Where(p => p.PaymentStatus == PaymentStatus.Processing)
                    .ToListAsync();

                if (!stuckPayments.Any())
                {
                    return Json(new { success = false, message = "No stuck payments found." });
                }

                var userName = User.Identity?.Name ?? "Admin";
                var fixedCount = 0;

                foreach (var transaction in stuckPayments)
                {
                    transaction.PaymentStatus = PaymentStatus.Completed;
                    transaction.CompletedDate = DateTime.Now;
                    transaction.ProcessedBy = userName;
                    transaction.ProcessedDate = DateTime.Now;
                    transaction.BankTransactionId = $"ADMIN-FIX-{transaction.PaymentTransactionId}";

                    if (transaction.Payroll != null)
                    {
                        transaction.Payroll.Status = PayrollStatus.Paid;
                    }

                    fixedCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"{fixedCount} stuck payments fixed by {userName}");

                return Json(new { success = true, message = $"{fixedCount} payment(s) fixed successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fixing all stuck payments: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
