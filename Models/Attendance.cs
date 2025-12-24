using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PayRollManagementSystem.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required(ErrorMessage = "Employee is required")]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Attendance Date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Check-in time is required")]
        [Display(Name = "Check-In Time")]
        public TimeSpan CheckInTime { get; set; }

        [Display(Name = "Check-Out Time")]
        public TimeSpan? CheckOutTime { get; set; }

        [Display(Name = "Total Hours")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? TotalHours { get; set; }

        [Required]
        [Display(Name = "Status")]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        [Display(Name = "Is Late")]
        public bool IsLate { get; set; } = false;

        [Display(Name = "Late By (Minutes)")]
        public int? LateByMinutes { get; set; }

        [Display(Name = "Is Half Day")]
        public bool IsHalfDay { get; set; } = false;

        [Display(Name = "Overtime Hours")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? OvertimeHours { get; set; }

        [StringLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Date Created")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }
    }

    public enum AttendanceStatus
    {
        Present,
        Absent,
        Late,
        [Display(Name = "On Leave")]
        OnLeave,
        Holiday,
        [Display(Name = "Half Day")]
        HalfDay
    }
}
