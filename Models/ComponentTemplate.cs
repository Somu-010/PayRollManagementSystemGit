using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    // Component Template - Pre-configured packages
    public class ComponentTemplate
    {
        [Key]
        public int TemplateId { get; set; }

        [Required(ErrorMessage = "Template name is required")]
        [StringLength(100)]
        [Display(Name = "Template Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Industry Type")]
        public IndustryType IndustryType { get; set; }

        [Required]
        [Display(Name = "Employee Level")]
        public EmployeeLevel EmployeeLevel { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Usage Count")]
        public int UsageCount { get; set; } = 0;

        [DataType(DataType.Date)]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<ComponentTemplateItem>? TemplateItems { get; set; }
    }

    // Template Items - Components in a template
    public class ComponentTemplateItem
    {
        [Key]
        public int TemplateItemId { get; set; }

        [Required]
        public int TemplateId { get; set; }

        [Required]
        public int AllowanceDeductionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Custom Value")]
        public decimal? CustomValue { get; set; } // Override component's default value

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        [ForeignKey("TemplateId")]
        public virtual ComponentTemplate? Template { get; set; }

        [ForeignKey("AllowanceDeductionId")]
        public virtual AllowanceDeduction? Component { get; set; }
    }

    // Enums
    public enum IndustryType
    {
        [Display(Name = "Information Technology")]
        IT,
        [Display(Name = "Manufacturing")]
        Manufacturing,
        [Display(Name = "Healthcare")]
        Healthcare,
        [Display(Name = "Finance & Banking")]
        Finance,
        [Display(Name = "Retail")]
        Retail,
        [Display(Name = "Education")]
        Education,
        [Display(Name = "Government")]
        Government,
        [Display(Name = "Startup")]
        Startup,
        [Display(Name = "Other")]
        Other
    }

    public enum EmployeeLevel
    {
        [Display(Name = "Entry Level")]
        EntryLevel,
        [Display(Name = "Junior")]
        Junior,
        [Display(Name = "Mid Level")]
        MidLevel,
        [Display(Name = "Senior")]
        Senior,
        [Display(Name = "Lead")]
        Lead,
        [Display(Name = "Manager")]
        Manager,
        [Display(Name = "Senior Manager")]
        SeniorManager,
        [Display(Name = "Director")]
        Director,
        [Display(Name = "Executive")]
        Executive
    }
}