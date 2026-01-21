using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int PaymentTransactionId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Transaction Number")]
        public string TransactionNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Payroll")]
        public int PayrollId { get; set; }

        [ForeignKey("PayrollId")]
        public virtual Payroll? Payroll { get; set; }

        // Company Bank Account (FROM) - The account money is paid from
        [Display(Name = "Company Bank Account")]
        public int? CompanyBankAccountId { get; set; }

        [ForeignKey("CompanyBankAccountId")]
        public virtual CompanyBankAccount? CompanyBankAccount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }

        // Employee Payment Details (TO)
        [StringLength(100)]
        [Display(Name = "Employee Bank Name")]
        public string? EmployeeBankName { get; set; }

        [StringLength(50)]
        [Display(Name = "Employee Account Number")]
        public string? EmployeeAccountNumber { get; set; }

        [StringLength(15)]
        [Display(Name = "Mobile Banking Number")]
        public string? MobileBankingNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "Mobile Banking Provider")]
        public string? MobileBankingProvider { get; set; }

        [StringLength(50)]
        [Display(Name = "Cheque Number")]
        public string? ChequeNumber { get; set; }

        [Required]
        [Display(Name = "Payment Status")]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [Display(Name = "Initiated Date")]
        public DateTime InitiatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Processed Date")]
        public DateTime? ProcessedDate { get; set; }

        [Display(Name = "Completed Date")]
        public DateTime? CompletedDate { get; set; }

        // SSLCommerz Integration Fields (if using gateway)
        [StringLength(100)]
        [Display(Name = "SSLCommerz Transaction ID")]
        public string? SslTransactionId { get; set; }

        [StringLength(100)]
        [Display(Name = "SSLCommerz Session ID")]
        public string? SslSessionId { get; set; }

        [StringLength(50)]
        [Display(Name = "Bank Transaction ID")]
        public string? BankTransactionId { get; set; }

        [StringLength(20)]
        [Display(Name = "Card Type")]
        public string? CardType { get; set; }

        [StringLength(10)]
        [Display(Name = "Card Brand")]
        public string? CardBrand { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Transaction Fee")]
        public decimal? TransactionFee { get; set; }

        [StringLength(500)]
        [Display(Name = "Gateway Response")]
        public string? GatewayResponse { get; set; }

        [StringLength(500)]
        [Display(Name = "Error Message")]
        public string? ErrorMessage { get; set; }

        [Display(Name = "Initiated By")]
        [StringLength(450)]
        public string InitiatedBy { get; set; } = string.Empty;

        [Display(Name = "Processed By")]
        [StringLength(450)]
        public string? ProcessedBy { get; set; }

        [Display(Name = "Approved By")]
        [StringLength(450)]
        public string? ApprovedBy { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        // Computed property
        [NotMapped]
        public string StatusDisplay => PaymentStatus.ToString().Replace("_", " ");

        [NotMapped]
        public string PaymentMethodDisplay => PaymentMethod.ToString().Replace("_", " ");
    }

    public enum PaymentMethod
    {
        [Display(Name = "SSLCommerz")]
        SSLCommerz,
        [Display(Name = "Bank Transfer")]
        BankTransfer,
        [Display(Name = "Mobile Banking")]
        MobileBanking,
        [Display(Name = "Cash")]
        Cash,
        [Display(Name = "Cheque")]
        Cheque
    }

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        [Display(Name = "Partially Completed")]
        Partially_Completed,
        [Display(Name = "Verification Required")]
        Verification_Required
    }
}
