using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Employee ID is required")]
        [StringLength(20)]
        [Display(Name = "Employee ID")]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(15)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required")]
        [StringLength(50)]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Designation is required")]
        [StringLength(50)]
        public string Designation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Basic salary is required")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Basic salary must be a positive number")]
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        [Required(ErrorMessage = "Joining date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; }

        [Required]
        [Display(Name = "Employment Status")]
        public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;

        [DataType(DataType.Date)]
        [Display(Name = "Date Created")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(10)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        // Foreign Key to Department
        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        // Navigation property to Department
        [ForeignKey("DepartmentId")]
        public virtual Department? DepartmentNavigation { get; set; }

        // Foreign Key to Designation (ADD THIS - IT'S MISSING!)
        [Display(Name = "Designation")]
        public int? DesignationId { get; set; }

        // Navigation property to Designation (ADD THIS - IT'S MISSING!)
        [ForeignKey("DesignationId")]
        public virtual Designation? DesignationNavigation { get; set; }

        // Foreign Key to Shift
        [Display(Name = "Shift")]
        public int? ShiftId { get; set; }

        // Navigation property to Shift
        [ForeignKey("ShiftId")]
        public virtual Shift? ShiftNavigation { get; set; }
    }

    public enum EmploymentStatus
    {
        Active,
        Inactive,
        Resigned
    }
}
