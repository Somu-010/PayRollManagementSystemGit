using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;
using PayRollManagementSystem.Services;
using System.Security.Claims;

namespace PayRollManagementSystem.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context,
            IPaymentService paymentService,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _logger = logger;
        }

        // GET: Payment/Index
        public async Task<IActionResult> Index(string status, int? month, int? year)
        {
            var query = _context.PaymentTransactions
                .Include(p => p.Payroll)
                    .ThenInclude(pr => pr.Employee)
                .Include(p => p.CompanyBankAccount)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PaymentStatus>(status, out var paymentStatus))
            {
                query = query.Where(p => p.PaymentStatus == paymentStatus);
            }

            if (month.HasValue)
            {
                query = query.Where(p => p.Payroll.Month == month.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(p => p.Payroll.Year == year.Value);
            }

            ViewData["CurrentStatus"] = status;
            ViewData["CurrentMonth"] = month;
            ViewData["CurrentYear"] = year;

            var transactions = await query
                .OrderByDescending(p => p.InitiatedDate)
                .ToListAsync();

            return View(transactions);
        }

        // GET: Payment/ProcessPayment/5
        public async Task<IActionResult> ProcessPayment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.PayrollId == id);

            if (payroll == null)
            {
                return NotFound();
            }

            if (payroll.Status != PayrollStatus.Approved)
            {
                TempData["Error"] = "Only approved payrolls can be paid!";
                return RedirectToAction("Details", "Payroll", new { id });
            }

            // Check if payment already exists
            var existingPayment = await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.PayrollId == id);

            if (existingPayment != null)
            {
                TempData["Error"] = "Payment already initiated for this payroll!";
                return RedirectToAction(nameof(Details), new { id = existingPayment.PaymentTransactionId });
            }

            // Get company bank accounts
            var companyBankAccounts = await _context.CompanyBankAccounts
                .Where(c => c.Status == BankAccountStatus.Active)
                .OrderByDescending(c => c.IsPrimary)
                .ToListAsync();

            if (!companyBankAccounts.Any())
            {
                TempData["Error"] = "No active company bank account found. Please add a company bank account first.";
                return RedirectToAction("Index", "CompanyBankAccount");
            }

            ViewBag.CompanyBankAccounts = companyBankAccounts;
            ViewBag.PrimaryAccount = companyBankAccounts.FirstOrDefault(c => c.IsPrimary);
            
            return View(payroll);
        }

        // POST: Payment/InitiatePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiatePayment(int payrollId, int? companyBankAccountId, PaymentMethod paymentMethod)
        {
            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.PayrollId == payrollId);

            if (payroll == null)
            {
                return NotFound();
            }

            // Get company bank account (use provided ID or get primary account)
            int selectedAccountId;
            if (companyBankAccountId.HasValue)
            {
                selectedAccountId = companyBankAccountId.Value;
            }
            else
            {
                var primaryAccount = await _context.CompanyBankAccounts
                    .FirstOrDefaultAsync(c => c.IsPrimary && c.Status == BankAccountStatus.Active);
                
                if (primaryAccount == null)
                {
                    TempData["Error"] = "No company bank account selected or found!";
                    return RedirectToAction(nameof(ProcessPayment), new { id = payrollId });
                }
                
                selectedAccountId = primaryAccount.CompanyBankAccountId;
            }

            var companyBankAccount = await _context.CompanyBankAccounts
                .FirstOrDefaultAsync(c => c.CompanyBankAccountId == selectedAccountId);

            if (companyBankAccount == null)
            {
                TempData["Error"] = "Company bank account not found!";
                return RedirectToAction(nameof(ProcessPayment), new { id = payrollId });
            }

            var transactionNumber = GenerateTransactionNumber(payroll);
            var userName = User.Identity?.Name ?? "System";

            // Create payment transaction with employee payment details
            var transaction = new PaymentTransaction
            {
                TransactionNumber = transactionNumber,
                PayrollId = payrollId,
                CompanyBankAccountId = selectedAccountId,
                Amount = payroll.NetSalary,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                InitiatedDate = DateTime.Now,
                InitiatedBy = userName
            };

            // Store employee payment details based on their preference
            var employee = payroll.Employee;
            if (employee != null)
            {
                if (employee.PaymentMethod == PaymentMethodPreference.BankTransfer)
                {
                    transaction.EmployeeBankName = employee.BankName;
                    transaction.EmployeeAccountNumber = employee.BankAccountNumber;
                }
                else if (employee.PaymentMethod == PaymentMethodPreference.MobileBanking)
                {
                    transaction.MobileBankingNumber = employee.MobileBankingNumber;
                    transaction.MobileBankingProvider = employee.MobileBankingProvider?.ToString();
                }
            }

            if (paymentMethod == PaymentMethod.SSLCommerz)
            {
                // Initiate SSLCommerz payment
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var sslRequest = new SSLCommerzRequest
                {
                    total_amount = payroll.NetSalary,
                    tran_id = transactionNumber,
                    success_url = $"{baseUrl}/Payment/PaymentSuccess",
                    fail_url = $"{baseUrl}/Payment/PaymentFailed",
                    cancel_url = $"{baseUrl}/Payment/PaymentCancelled",
                    ipn_url = $"{baseUrl}/Payment/IPN",
                    cus_name = employee.Name,
                    cus_email = employee.Email,
                    cus_add1 = employee.Address ?? "N/A",
                    cus_city = employee.City ?? "Dhaka",
                    cus_postcode = employee.PostalCode ?? "1000",
                    cus_phone = employee.Phone,
                    product_name = $"Salary Payment - {payroll.PayPeriod}",
                    value_a = payrollId.ToString(),
                    value_b = payroll.EmployeeId.ToString(),
                    value_c = transactionNumber
                };

                var sslResponse = await _paymentService.InitiatePayment(sslRequest);

                if (sslResponse.status == "SUCCESS")
                {
                    transaction.SslSessionId = sslResponse.sessionkey;
                    transaction.PaymentStatus = PaymentStatus.Processing;
                    
                    _context.PaymentTransactions.Add(transaction);
                    await _context.SaveChangesAsync();

                    return Redirect(sslResponse.GatewayPageURL);
                }
                else
                {
                    transaction.PaymentStatus = PaymentStatus.Failed;
                    transaction.ErrorMessage = sslResponse.failedreason;
                    
                    _context.PaymentTransactions.Add(transaction);
                    await _context.SaveChangesAsync();

                    TempData["Error"] = $"Failed to initiate payment: {sslResponse.failedreason}";
                    return RedirectToAction(nameof(ProcessPayment), new { id = payrollId });
                }
            }
            else
            {
                // For manual payment methods
                transaction.PaymentStatus = PaymentStatus.Pending;
                
                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Payment initiated successfully!";
                return RedirectToAction(nameof(Details), new { id = transaction.PaymentTransactionId });
            }
        }

        // GET: Payment/PaymentSuccess
        [AllowAnonymous] // Allow SSLCommerz to redirect here without authentication
        public async Task<IActionResult> PaymentSuccess()
        {
            var valId = Request.Query["val_id"].ToString();
            var tranId = Request.Query["tran_id"].ToString();

            _logger.LogInformation($"PaymentSuccess called - val_id: {valId}, tran_id: {tranId}");

            if (string.IsNullOrEmpty(valId) || string.IsNullOrEmpty(tranId))
            {
                _logger.LogWarning("Invalid payment response - missing val_id or tran_id");
                TempData["Error"] = "Invalid payment response!";
                return RedirectToAction(nameof(Index));
            }

            // Validate transaction with SSLCommerz
            var validationResponse = await _paymentService.ValidateTransaction(valId);

            _logger.LogInformation($"Validation status: {validationResponse.status}");

            if (validationResponse.status == "VALID" || validationResponse.status == "VALIDATED")
            {
                var transaction = await _context.PaymentTransactions
                    .Include(p => p.Payroll)
                    .Include(p => p.CompanyBankAccount)
                    .FirstOrDefaultAsync(p => p.TransactionNumber == tranId);

                if (transaction != null)
                {
                    transaction.PaymentStatus = PaymentStatus.Completed;
                    transaction.CompletedDate = DateTime.Now;
                    transaction.SslTransactionId = validationResponse.tran_id;
                    transaction.BankTransactionId = validationResponse.bank_tran_id;
                    transaction.CardType = validationResponse.card_type;
                    transaction.CardBrand = validationResponse.card_brand;
                    transaction.ProcessedBy = User.Identity?.Name ?? "SSLCommerz Gateway";
                    transaction.ProcessedDate = DateTime.Now;

                    // Update payroll status
                    if (transaction.Payroll != null)
                    {
                        transaction.Payroll.Status = PayrollStatus.Paid;
                    }

                    // ? AUTOMATIC BALANCE REDUCTION - When payment completes
                    if (transaction.CompanyBankAccount != null && transaction.CompanyBankAccount.AvailableBalance.HasValue)
                    {
                        var previousBalance = transaction.CompanyBankAccount.AvailableBalance.Value;
                        transaction.CompanyBankAccount.AvailableBalance -= transaction.Amount;
                        transaction.CompanyBankAccount.UpdatedAt = DateTime.Now;
                        
                        _logger.LogInformation(
                            $"?? AUTO BALANCE REDUCTION: Account '{transaction.CompanyBankAccount.AccountName}' - " +
                            $"Previous: {previousBalance:N2}, Deducted: {transaction.Amount:N2}, " +
                            $"New Balance: {transaction.CompanyBankAccount.AvailableBalance:N2}");
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Transaction {tranId} marked as completed successfully");

                    TempData["Success"] = "Payment completed successfully!";
                    return RedirectToAction(nameof(Details), new { id = transaction.PaymentTransactionId });
                }
                else
                {
                    _logger.LogWarning($"Transaction not found for tran_id: {tranId}");
                }
            }

            TempData["Error"] = "Payment validation failed!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Payment/PaymentFailed
        [AllowAnonymous] // Allow SSLCommerz to redirect here without authentication
        public async Task<IActionResult> PaymentFailed()
        {
            try
            {
                var tranId = Request.Query["tran_id"].ToString();
                var error = Request.Query["error"].ToString();

                _logger.LogWarning($"Payment failed for transaction: {tranId}, Error: {error}");

                if (!string.IsNullOrEmpty(tranId))
                {
                    var transaction = await _context.PaymentTransactions
                        .FirstOrDefaultAsync(p => p.TransactionNumber == tranId);

                    if (transaction != null)
                    {
                        transaction.PaymentStatus = PaymentStatus.Failed;
                        transaction.ErrorMessage = !string.IsNullOrEmpty(error) 
                            ? error 
                            : "Payment failed by user or gateway";
                        await _context.SaveChangesAsync();

                        // Redirect to transaction details to show the error
                        TempData["Error"] = $"Payment failed: {transaction.ErrorMessage}";
                        return RedirectToAction(nameof(Details), new { id = transaction.PaymentTransactionId });
                    }
                }

                TempData["Error"] = "Payment failed!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling payment failure: {ex.Message}");
                TempData["Error"] = "An error occurred while processing the payment failure.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Payment/PaymentCancelled
        [AllowAnonymous] // Allow SSLCommerz to redirect here without authentication
        public async Task<IActionResult> PaymentCancelled()
        {
            var tranId = Request.Query["tran_id"].ToString();

            _logger.LogInformation($"Payment cancelled for transaction: {tranId}");

            if (!string.IsNullOrEmpty(tranId))
            {
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(p => p.TransactionNumber == tranId);

                if (transaction != null)
                {
                    transaction.PaymentStatus = PaymentStatus.Cancelled;
                    transaction.ErrorMessage = "Payment cancelled by user";
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Warning"] = "Payment was cancelled!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Payment/IPN (Instant Payment Notification)
        [HttpPost]
        [AllowAnonymous] // Allow SSLCommerz to send IPN without authentication
        public async Task<IActionResult> IPN()
        {
            // SSLCommerz will send IPN notifications here
            var valId = Request.Form["val_id"].ToString();
            var tranId = Request.Form["tran_id"].ToString();

            _logger.LogInformation($"IPN received for transaction: {tranId}");

            // Validate and update transaction status
            if (!string.IsNullOrEmpty(valId))
            {
                var validationResponse = await _paymentService.ValidateTransaction(valId);
                
                if (validationResponse.status == "VALID")
                {
                    var transaction = await _context.PaymentTransactions
                        .Include(p => p.Payroll)
                        .FirstOrDefaultAsync(p => p.TransactionNumber == tranId);

                    if (transaction != null && transaction.PaymentStatus != PaymentStatus.Completed)
                    {
                        transaction.PaymentStatus = PaymentStatus.Completed;
                        transaction.CompletedDate = DateTime.Now;
                        transaction.BankTransactionId = validationResponse.bank_tran_id;

                        if (transaction.Payroll != null)
                        {
                            transaction.Payroll.Status = PayrollStatus.Paid;
                        }

                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"IPN: Transaction {tranId} marked as completed");
                    }
                }
            }

            return Ok();
        }

        // GET: Payment/Details/5
        [AllowAnonymous] // Allow viewing payment details after SSLCommerz redirect
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.PaymentTransactions
                .Include(p => p.Payroll)
                    .ThenInclude(pr => pr.Employee)
                .Include(p => p.CompanyBankAccount)
                .FirstOrDefaultAsync(p => p.PaymentTransactionId == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // POST: Payment/MarkAsCompleted
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int id, string bankTransactionId, string remarks)
        {
            var transaction = await _context.PaymentTransactions
                .Include(p => p.Payroll)
                .Include(p => p.CompanyBankAccount)
                .FirstOrDefaultAsync(p => p.PaymentTransactionId == id);

            if (transaction == null)
            {
                return Json(new { success = false, message = "Transaction not found." });
            }

            if (transaction.PaymentMethod == PaymentMethod.SSLCommerz)
            {
                return Json(new { success = false, message = "SSLCommerz payments are automatically completed." });
            }

            transaction.PaymentStatus = PaymentStatus.Completed;
            transaction.CompletedDate = DateTime.Now;
            transaction.BankTransactionId = bankTransactionId;
            transaction.Remarks = remarks;
            transaction.ProcessedBy = User.Identity?.Name ?? "System";
            transaction.ProcessedDate = DateTime.Now;

            if (transaction.Payroll != null)
            {
                transaction.Payroll.Status = PayrollStatus.Paid;
            }

            // ? AUTOMATIC BALANCE REDUCTION - When manual payment is marked complete
            if (transaction.CompanyBankAccount != null && transaction.CompanyBankAccount.AvailableBalance.HasValue)
            {
                var previousBalance = transaction.CompanyBankAccount.AvailableBalance.Value;
                transaction.CompanyBankAccount.AvailableBalance -= transaction.Amount;
                transaction.CompanyBankAccount.UpdatedAt = DateTime.Now;
                
                _logger.LogInformation(
                    $"?? AUTO BALANCE REDUCTION (Manual): Account '{transaction.CompanyBankAccount.AccountName}' - " +
                    $"Previous: {previousBalance:N2}, Deducted: {transaction.Amount:N2}, " +
                    $"New Balance: {transaction.CompanyBankAccount.AvailableBalance:N2}");
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Payment marked as completed successfully!" });
        }

        private string GenerateTransactionNumber(Payroll payroll)
        {
            return $"TXN-{DateTime.Now:yyyyMMddHHmmss}-{payroll.EmployeeId}";
        }
    }
}
