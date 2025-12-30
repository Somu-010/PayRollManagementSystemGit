using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    public class PayrollDetail
    {
        [Key]
        public int PayrollDetailId { get; set; }

        [Required]
        public int PayrollId { get; set; }

        [ForeignKey("PayrollId")]
        public virtual Payroll? Payroll { get; set; }

        [Required]
        public int? AllowanceDeductionId { get; set; }

        [ForeignKey("AllowanceDeductionId")]
        public virtual AllowanceDeduction? AllowanceDeduction { get; set; }

        [Required]
        [Display(Name = "Component Name")]
        [StringLength(100)]
        public string ComponentName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Component Type")]
        public ComponentType ComponentType { get; set; }

        [Required]
        [Display(Name = "Calculation Method")]
        public CalculationMethod CalculationMethod { get; set; }

        [Display(Name = "Component Value")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ComponentValue { get; set; }

        [Required]
        [Display(Name = "Calculated Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Display(Name = "Is Taxable")]
        public bool IsTaxable { get; set; }

        [Display(Name = "Remarks")]
        [StringLength(500)]
        public string? Remarks { get; set; }
    }
}
