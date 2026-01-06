using System.ComponentModel.DataAnnotations;

namespace PayRollManagementSystem.Models
{
    public class CompanySetting
    {
        [Key]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Company Address")]
        public string? Address { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [StringLength(100)]
        [Display(Name = "Website")]
        public string? Website { get; set; }

        [StringLength(50)]
        [Display(Name = "Tax/Registration Number")]
        public string? TaxNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Logo Path")]
        public string? LogoPath { get; set; }

        [StringLength(100)]
        [Display(Name = "Currency")]
        public string Currency { get; set; } = "USD";

        [StringLength(10)]
        [Display(Name = "Currency Symbol")]
        public string CurrencySymbol { get; set; } = "$";

        [Display(Name = "Fiscal Year Start Month")]
        [Range(1, 12)]
        public int FiscalYearStartMonth { get; set; } = 1;

        [StringLength(100)]
        [Display(Name = "Timezone")]
        public string? Timezone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
