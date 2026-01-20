using System.ComponentModel.DataAnnotations;

namespace PayRollManagementSystem.Models
{
    public class WeekendSetting
    {
        [Key]
        public int WeekendSettingId { get; set; }

        [Display(Name = "Friday is Weekend")]
        public bool IsFridayWeekend { get; set; } = true;

        [Display(Name = "Saturday is Weekend")]
        public bool IsSaturdayWeekend { get; set; } = false;

        [Display(Name = "Sunday is Weekend")]
        public bool IsSundayWeekend { get; set; } = false;

        [Display(Name = "Monday is Weekend")]
        public bool IsMondayWeekend { get; set; } = false;

        [Display(Name = "Tuesday is Weekend")]
        public bool IsTuesdayWeekend { get; set; } = false;

        [Display(Name = "Wednesday is Weekend")]
        public bool IsWednesdayWeekend { get; set; } = false;

        [Display(Name = "Thursday is Weekend")]
        public bool IsThursdayWeekend { get; set; } = false;

        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.Now;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }

        // Helper method to get weekend days as list
        public List<DayOfWeek> GetWeekendDays()
        {
            var weekendDays = new List<DayOfWeek>();

            if (IsSundayWeekend) weekendDays.Add(DayOfWeek.Sunday);
            if (IsMondayWeekend) weekendDays.Add(DayOfWeek.Monday);
            if (IsTuesdayWeekend) weekendDays.Add(DayOfWeek.Tuesday);
            if (IsWednesdayWeekend) weekendDays.Add(DayOfWeek.Wednesday);
            if (IsThursdayWeekend) weekendDays.Add(DayOfWeek.Thursday);
            if (IsFridayWeekend) weekendDays.Add(DayOfWeek.Friday);
            if (IsSaturdayWeekend) weekendDays.Add(DayOfWeek.Saturday);

            return weekendDays;
        }

        // Helper method to check if a specific day is weekend
        public bool IsWeekend(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => IsSundayWeekend,
                DayOfWeek.Monday => IsMondayWeekend,
                DayOfWeek.Tuesday => IsTuesdayWeekend,
                DayOfWeek.Wednesday => IsWednesdayWeekend,
                DayOfWeek.Thursday => IsThursdayWeekend,
                DayOfWeek.Friday => IsFridayWeekend,
                DayOfWeek.Saturday => IsSaturdayWeekend,
                _ => false
            };
        }

        // Get display text for active weekend days
        public string GetWeekendDaysDisplay()
        {
            var days = new List<string>();
            if (IsFridayWeekend) days.Add("Friday");
            if (IsSaturdayWeekend) days.Add("Saturday");
            if (IsSundayWeekend) days.Add("Sunday");
            if (IsMondayWeekend) days.Add("Monday");
            if (IsTuesdayWeekend) days.Add("Tuesday");
            if (IsWednesdayWeekend) days.Add("Wednesday");
            if (IsThursdayWeekend) days.Add("Thursday");

            return days.Any() ? string.Join(", ", days) : "No weekends set";
        }
    }
}
