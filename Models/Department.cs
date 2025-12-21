using System.ComponentModel.DataAnnotations;

namespace PayRollManagementSystem.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Department code is required")]
        [StringLength(20)]
        [Display(Name = "Department Code")]
        public string DepartmentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department name is required")]
        [StringLength(100)]
        [Display(Name = "Department Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Head of department is required")]
        [StringLength(100)]
        [Display(Name = "Department Head")]
        public string HeadOfDepartment { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(15)]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "Status")]
        public DepartmentStatus Status { get; set; } = DepartmentStatus.Active;

        [DataType(DataType.Date)]
        [Display(Name = "Established Date")]
        public DateTime? EstablishedDate { get; set; }

        [Display(Name = "Employee Count")]
        public int EmployeeCount { get; set; } = 0;

        [DataType(DataType.Date)]
        [Display(Name = "Date Created")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<Employee>? Employees { get; set; }
    }

    public enum DepartmentStatus
    {
        Active,
        Inactive
    }
}