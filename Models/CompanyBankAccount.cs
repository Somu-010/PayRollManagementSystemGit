using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    public class CompanyBankAccount
    {
        [Key]
        public int CompanyBankAccountId { get; set; }

        [Required(ErrorMessage = "Account name is required")]
        [StringLength(100)]
        [Display(Name = "Account Name")]
        public string AccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(100)]
        [Display(Name = "Bank Name")]
        public string BankName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Branch Name")]
        public string? BranchName { get; set; }

        [Required(ErrorMessage = "Account number is required")]
        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Routing Number")]
        public string? RoutingNumber { get; set; }

        [Required]
        [Display(Name = "Account Type")]
        public CompanyAccountType AccountType { get; set; }

        [StringLength(20)]
        [Display(Name = "SWIFT Code")]
        public string? SwiftCode { get; set; }

        [Display(Name = "Primary Account")]
        public bool IsPrimary { get; set; }

        [Required]
        [Display(Name = "Status")]
        public BankAccountStatus Status { get; set; } = BankAccountStatus.Active;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Available Balance")]
        public decimal? AvailableBalance { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<PaymentTransaction>? PaymentTransactions { get; set; }
    }

    public enum CompanyAccountType
    {
        [Display(Name = "Current Account")]
        Current = 0,
        [Display(Name = "Savings Account")]
        Savings = 1,
        [Display(Name = "Payroll Account")]
        Payroll = 2
    }

    public enum BankAccountStatus
    {
        [Display(Name = "Active")]
        Active = 0,
        [Display(Name = "Inactive")]
        Inactive = 1,
        [Display(Name = "Suspended")]
        Suspended = 2
    }
}
