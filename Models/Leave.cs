using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    public class Leave
    {
        [Key]
        public int LeaveId { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Leave type is required")]
        [Display(Name = "Leave Type")]
        public LeaveType LeaveType { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Number of Days")]
        public int NumberOfDays { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(1000)]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        [StringLength(500)]
        [Display(Name = "Admin Remarks")]
        public string? AdminRemarks { get; set; }

        [Display(Name = "Approved/Rejected By")]
        public string? ApprovedBy { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Action Date")]
        public DateTime? ActionDate { get; set; }

        [Display(Name = "Is Half Day")]
        public bool IsHalfDay { get; set; } = false;

        [DataType(DataType.Date)]
        [Display(Name = "Applied On")]
        public DateTime AppliedOn { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }
    }

    public enum LeaveType
    {
        [Display(Name = "Casual Leave")]
        CasualLeave,

        [Display(Name = "Sick Leave")]
        SickLeave,

        [Display(Name = "Annual Leave")]
        AnnualLeave,

        [Display(Name = "Maternity Leave")]
        MaternityLeave,

        [Display(Name = "Unpaid Leave")]
        UnpaidLeave
    }

    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }
}