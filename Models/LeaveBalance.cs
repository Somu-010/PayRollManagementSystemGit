using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    public class LeaveBalance
    {
        [Key]
        public int LeaveBalanceId { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [Display(Name = "Year")]
        public int Year { get; set; }

        [Display(Name = "Casual Leave Balance")]
        public decimal CasualLeaveBalance { get; set; }

        [Display(Name = "Casual Leave Used")]
        public decimal CasualLeaveUsed { get; set; }

        [Display(Name = "Sick Leave Balance")]
        public decimal SickLeaveBalance { get; set; }

        [Display(Name = "Sick Leave Used")]
        public decimal SickLeaveUsed { get; set; }

        [Display(Name = "Annual Leave Balance")]
        public decimal AnnualLeaveBalance { get; set; }

        [Display(Name = "Annual Leave Used")]
        public decimal AnnualLeaveUsed { get; set; }

        [Display(Name = "Maternity Leave Balance")]
        public decimal MaternityLeaveBalance { get; set; }

        [Display(Name = "Maternity Leave Used")]
        public decimal MaternityLeaveUsed { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        // Calculated properties
        [NotMapped]
        public decimal TotalCasualLeave => CasualLeaveBalance;

        [NotMapped]
        public decimal RemainingCasualLeave => CasualLeaveBalance - CasualLeaveUsed;

        [NotMapped]
        public decimal TotalSickLeave => SickLeaveBalance;

        [NotMapped]
        public decimal RemainingSickLeave => SickLeaveBalance - SickLeaveUsed;

        [NotMapped]
        public decimal TotalAnnualLeave => AnnualLeaveBalance;

        [NotMapped]
        public decimal RemainingAnnualLeave => AnnualLeaveBalance - AnnualLeaveUsed;

        [NotMapped]
        public decimal TotalMaternityLeave => MaternityLeaveBalance;

        [NotMapped]
        public decimal RemainingMaternityLeave => MaternityLeaveBalance - MaternityLeaveUsed;
    }
}