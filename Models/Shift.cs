using System.ComponentModel.DataAnnotations;

namespace PayRollManagementSystem.Models
{
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        [Required(ErrorMessage = "Shift code is required")]
        [StringLength(20)]
        [Display(Name = "Shift Code")]
        public string ShiftCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shift name is required")]
        [StringLength(100)]
        [Display(Name = "Shift Name")]
        public string ShiftName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Break duration is required")]
        [Display(Name = "Break Duration (minutes)")]
        [Range(0, 480, ErrorMessage = "Break duration must be between 0 and 480 minutes")]
        public int BreakDuration { get; set; }

        [Required(ErrorMessage = "Grace period is required")]
        [Display(Name = "Grace Period (minutes)")]
        [Range(0, 60, ErrorMessage = "Grace period must be between 0 and 60 minutes")]
        public int GracePeriod { get; set; }

        [Required(ErrorMessage = "Late mark after is required")]
        [Display(Name = "Late Mark After (minutes)")]
        [Range(0, 120, ErrorMessage = "Late mark must be between 0 and 120 minutes")]
        public int LateMarkAfter { get; set; }

        [Required(ErrorMessage = "Half day hours is required")]
        [Display(Name = "Half Day Hours")]
        [Range(0, 12, ErrorMessage = "Half day hours must be between 0 and 12")]
        public decimal HalfDayHours { get; set; }

        [Required(ErrorMessage = "Full day hours is required")]
        [Display(Name = "Full Day Hours")]
        [Range(0, 24, ErrorMessage = "Full day hours must be between 0 and 24")]
        public decimal FullDayHours { get; set; }

        [Required]
        [Display(Name = "Status")]
        public ShiftStatus Status { get; set; } = ShiftStatus.Active;

        [Display(Name = "Assigned Employees")]
        public int AssignedEmployees { get; set; } = 0;

        [Display(Name = "Is Night Shift")]
        public bool IsNightShift { get; set; } = false;

        [Display(Name = "Weekend Shift")]
        public bool IsWeekendShift { get; set; } = false;

        [DataType(DataType.Date)]
        [Display(Name = "Date Created")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        [Display(Name = "Last Updated")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<Employee>? Employees { get; set; }
    }

    public enum ShiftStatus
    {
        Active,
        Inactive
    }
}