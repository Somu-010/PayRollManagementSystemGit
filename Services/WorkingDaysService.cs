using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace PayRollManagementSystem.Services
{
    public class WorkingDaysService
    {
        private readonly ApplicationDbContext _context;
        private readonly HolidayService _holidayService;

        public WorkingDaysService(ApplicationDbContext context, HolidayService holidayService)
        {
            _context = context;
            _holidayService = holidayService;
        }

        /// <summary>
        /// Calculate working days in a month excluding weekends and holidays
        /// </summary>
        public async Task<int> GetWorkingDaysInMonth(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Get holidays for the month
            var holidays = await _holidayService.GetHolidaysForMonth(year, month);
            var holidayDates = holidays.Select(h => h.Date.Date).ToHashSet();

            int workingDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Check if it's a weekend (Saturday or Sunday)
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue; // Skip weekends
                }

                // Check if it's a holiday
                if (holidayDates.Contains(date.Date))
                {
                    continue; // Skip holidays
                }

                workingDays++;
            }

            return workingDays;
        }

        /// <summary>
        /// Calculate working days between two dates excluding weekends and holidays
        /// </summary>
        public async Task<int> GetWorkingDaysBetweenDates(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
            {
                return 0;
            }

            // Get all holidays in the date range
            var holidays = await _context.Holidays
                .Where(h => h.IsActive && h.Date >= startDate && h.Date <= endDate)
                .Select(h => h.Date.Date)
                .ToListAsync();

            var holidayDates = holidays.ToHashSet();

            int workingDays = 0;
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                // Skip weekends
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                // Skip holidays
                if (holidayDates.Contains(date))
                {
                    continue;
                }

                workingDays++;
            }

            return workingDays;
        }

        /// <summary>
        /// Check if a date is a working day (not weekend, not holiday)
        /// </summary>
        public async Task<bool> IsWorkingDay(DateTime date)
        {
            // Check if weekend
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }

            // Check if holiday
            var isHoliday = await _holidayService.IsHoliday(date);
            return !isHoliday;
        }

        /// <summary>
        /// Get total calendar days in a month
        /// </summary>
        public int GetTotalDaysInMonth(int year, int month)
        {
            return DateTime.DaysInMonth(year, month);
        }

        /// <summary>
        /// Get weekend days in a month
        /// </summary>
        public int GetWeekendDaysInMonth(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            int weekendDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    weekendDays++;
                }
            }

            return weekendDays;
        }

        /// <summary>
        /// Calculate attendance summary for an employee in a month
        /// </summary>
        public async Task<AttendanceSummary> CalculateAttendanceSummary(int employeeId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Get all attendance records for the month
            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();

            // Get working days in month
            var totalWorkingDays = await GetWorkingDaysInMonth(year, month);
            var totalCalendarDays = GetTotalDaysInMonth(year, month);
            var weekendDays = GetWeekendDaysInMonth(year, month);
            
            // Get holidays
            var holidays = await _holidayService.GetHolidaysForMonth(year, month);
            var holidayCount = holidays.Count;

            // Calculate attendance statistics
            var presentDays = attendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
            var lateDays = attendances.Count(a => a.IsLate);
            var halfDays = attendances.Count(a => a.IsHalfDay);
            var leaveDays = attendances.Count(a => a.Status == AttendanceStatus.OnLeave);
            var markedAbsentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent);

            // Calculate hours
            var totalHours = attendances.Sum(a => a.TotalHours ?? 0);
            var overtimeHours = attendances.Sum(a => a.OvertimeHours ?? 0);

            // Calculate total absent days
            // Unmarked days (working days without attendance) are automatically counted as absent
            var markedWorkingDays = presentDays + markedAbsentDays + leaveDays;
            var unmarkedDays = totalWorkingDays - markedWorkingDays;
            if (unmarkedDays < 0) unmarkedDays = 0;

            // Total absent days = explicitly marked absent + unmarked days
            var totalAbsentDays = markedAbsentDays + unmarkedDays;

            // Calculate attendance percentage based on present days only
            var attendancePercentage = totalWorkingDays > 0 
                ? (decimal)presentDays / totalWorkingDays * 100 
                : 0;

            return new AttendanceSummary
            {
                EmployeeId = employeeId,
                Year = year,
                Month = month,
                TotalCalendarDays = totalCalendarDays,
                TotalWorkingDays = totalWorkingDays,
                WeekendDays = weekendDays,
                HolidayDays = holidayCount,
                PresentDays = presentDays,
                AbsentDays = totalAbsentDays,  // Includes unmarked days
                LateDays = lateDays,
                HalfDays = halfDays,
                LeaveDays = leaveDays,
                UnmarkedDays = unmarkedDays,  // For reporting purposes only
                TotalHours = totalHours,
                OvertimeHours = overtimeHours,
                AttendancePercentage = attendancePercentage,
                Attendances = attendances
            };
        }
    }

    /// <summary>
    /// Attendance summary for reporting
    /// </summary>
    public class AttendanceSummary
    {
        public int EmployeeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        
        // Calendar Information
        public int TotalCalendarDays { get; set; }  // Total days in month (e.g., 31)
        public int TotalWorkingDays { get; set; }   // Working days excluding weekends and holidays
        public int WeekendDays { get; set; }        // Saturday + Sunday count
        public int HolidayDays { get; set; }        // Public holidays count
        
        // Attendance Statistics
        public int PresentDays { get; set; }        // Including late arrivals
        public int AbsentDays { get; set; }         // Days marked absent + unmarked days
        public int LateDays { get; set; }           // Days with late arrival
        public int HalfDays { get; set; }           // Half day attendance
        public int LeaveDays { get; set; }          // Days on approved leave
        public int UnmarkedDays { get; set; }       // Working days without attendance (for reference)
        
        // Hours Information
        public decimal TotalHours { get; set; }     // Total working hours
        public decimal OvertimeHours { get; set; }  // Total overtime hours
        
        // Performance Metrics
        public decimal AttendancePercentage { get; set; }
        
        // Attendance Records
        public List<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
