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
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Check-in time is required")]
        [Display(Name = "Check-In Time")]
        public TimeSpan CheckInTime { get; set; }

        [Display(Name = "Check-Out Time")]
        public TimeSpan? CheckOutTime { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Total Hours")]
        public decimal? TotalHours { get; set; }

        [Required]
        [Display(Name = "Status")]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        [Display(Name = "Late Arrival")]
        public bool IsLate { get; set; } = false;

        [Display(Name = "Late By (Minutes)")]
        public int? LateByMinutes { get; set; }

        [Display(Name = "Half Day")]
        public bool IsHalfDay { get; set; } = false;

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Overtime Hours")]
        public decimal? OvertimeHours { get; set; }

        [StringLength(500)]
        [Display(Name = "Remarks/Notes")]
        public string? Remarks { get; set; }

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

    public enum AttendanceStatus
    {
        Present,
        Absent,
        Late,
        OnLeave,
        HalfDay
    }
}