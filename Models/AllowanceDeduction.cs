using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    public class AllowanceDeduction
    {
        [Key]
        public int AllowanceDeductionId { get; set; }

        [Required(ErrorMessage = "Code is required")]
        [StringLength(20)]
        [Display(Name = "Code")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Type")]
        public ComponentType Type { get; set; }

        [Required]
        [Display(Name = "Calculation Method")]
        public CalculationMethod CalculationMethod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount/Percentage")]
        [Range(0, double.MaxValue, ErrorMessage = "Value must be positive")]
        public decimal Value { get; set; }

        [Display(Name = "Is Taxable")]
        public bool IsTaxable { get; set; } = false;

        [Display(Name = "Is Mandatory")]
        public bool IsMandatory { get; set; } = false;

        [Display(Name = "Applies to All Employees")]
        public bool AppliesToAll { get; set; } = false;

        [Display(Name = "Minimum Salary Threshold")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumSalaryThreshold { get; set; }

        [Display(Name = "Maximum Amount Cap")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumCap { get; set; }

        [Required]
        [Display(Name = "Status")]
        public ComponentStatus Status { get; set; } = ComponentStatus.Active;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [DataType(DataType.Date)]
        [Display(Name = "Effective From")]
        public DateTime? EffectiveFrom { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Effective Until")]
        public DateTime? EffectiveUntil { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // Calculated properties for display
        [NotMapped]
        public string TypeDisplay => Type == ComponentType.Allowance ? "Allowance" : "Deduction";

        [NotMapped]
        public string CalculationDisplay => CalculationMethod == CalculationMethod.FixedAmount
            ? $"${Value:N2}"
            : $"{Value}%";
    }

    public enum ComponentType
    {
        Allowance,
        Deduction
    }

    public enum CalculationMethod
    {
        [Display(Name = "Fixed Amount")]
        FixedAmount,

        [Display(Name = "Percentage of Basic Salary")]
        PercentageOfBasic,

        [Display(Name = "Percentage of Gross Salary")]
        PercentageOfGross
    }

    public enum ComponentStatus
    {
        Active,
        Inactive
    }
}