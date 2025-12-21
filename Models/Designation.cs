using System.ComponentModel.DataAnnotations;

namespace PayRollManagementSystem.Models
{
    public class Designation
    {
        [Key]
        public int DesignationId { get; set; }

        [Required(ErrorMessage = "Designation code is required")]
        [StringLength(20)]
        [Display(Name = "Designation Code")]
        public string DesignationCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Designation title is required")]
        [StringLength(100)]
        [Display(Name = "Designation Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [StringLength(50)]
        [Display(Name = "Level")]
        public string? Level { get; set; } // e.g., Entry, Mid, Senior, Executive

        [Display(Name = "Minimum Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Minimum salary must be a positive number")]
        public decimal? MinimumSalary { get; set; }

        [Display(Name = "Maximum Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Maximum salary must be a positive number")]
        public decimal? MaximumSalary { get; set; }

        [Required]
        [Display(Name = "Status")]
        public DesignationStatus Status { get; set; } = DesignationStatus.Active;

        [Display(Name = "Employee Count")]
        public int EmployeeCount { get; set; } = 0;

        [DataType(DataType.Date)]
        [Display(Name = "Date Created")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [Display(Name = "Department")]
        public virtual Department? Department { get; set; }

        public virtual ICollection<Employee>? Employees { get; set; }
    }

    public enum DesignationStatus
    {
        Active,
        Inactive
    }
}