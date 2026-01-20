using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

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
        /// Get active weekend setting
        /// </summary>
        private async Task<WeekendSetting?> GetActiveWeekendSetting()
        {
            return await _context.WeekendSettings
                .Where(w => w.IsActive)
                .OrderByDescending(w => w.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Check if a specific date is a weekend
        /// </summary>
        private async Task<bool> IsWeekend(DateTime date)
        {
            var weekendSetting = await GetActiveWeekendSetting();
            
            if (weekendSetting == null)
            {
                // Default to Friday-Saturday if no setting exists (Bangladesh default)
                return date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday;
            }

            return weekendSetting.IsWeekend(date.DayOfWeek);
        }

        /// <summary>
        /// Calculate working days between two dates (inclusive)
        /// </summary>
        public async Task<int> GetWorkingDaysBetween(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentException("Start date must be before or equal to end date");
            }

            var holidays = await _context.Holidays
                .Where(h => h.IsActive && h.Date >= startDate && h.Date <= endDate)
                .Select(h => h.Date.Date)
                .ToListAsync();

            var holidayDates = holidays.ToHashSet();

            int workingDays = 0;
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                // Skip weekends
                if (await IsWeekend(date))
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
        /// Calculate total working days in a specific month
        /// </summary>
        public async Task<int> GetWorkingDaysInMonth(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            return await GetWorkingDaysBetween(startDate, endDate);
        }

        /// <summary>
        /// Check if a date is a working day (not weekend, not holiday)
        /// </summary>
        public async Task<bool> IsWorkingDay(DateTime date)
        {
            // Check if weekend
            if (await IsWeekend(date))
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
        public async Task<int> GetWeekendDaysInMonth(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            int weekendDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (await IsWeekend(date))
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
            var weekendDays = await GetWeekendDaysInMonth(year, month);
            
            // Get holidays
            var holidays = await _holidayService.GetHolidaysForMonth(year, month);
            var holidayCount = holidays.Count;

            // Calculate attendance statistics
            // Present = Actually present + Late + OnLeave (all count as "not absent")
            var presentDays = attendances.Count(a => a.Status == AttendanceStatus.Present || 
                                                     a.Status == AttendanceStatus.Late || 
                                                     a.Status == AttendanceStatus.OnLeave);
            
            var lateDays = attendances.Count(a => a.IsLate);
            var leaveDays = attendances.Count(a => a.Status == AttendanceStatus.OnLeave);
            var halfDays = attendances.Count(a => a.Status == AttendanceStatus.HalfDay);
            
            // Manually marked absences
            var markedAbsent = attendances.Count(a => a.Status == AttendanceStatus.Absent);
            
            // Unmarked days = Total working days - Days with any attendance record
            var markedDays = attendances.Count;
            var unmarkedDays = totalWorkingDays - markedDays;
            
            // Total absent = Marked absent + Unmarked days
            var absentDays = markedAbsent + unmarkedDays;

            // Calculate total worked hours and overtime
            var totalWorkedHours = attendances
                .Where(a => a.TotalHours.HasValue)
                .Sum(a => a.TotalHours.Value);
            
            var overtimeHours = attendances
                .Where(a => a.OvertimeHours.HasValue)
                .Sum(a => a.OvertimeHours.Value);

            // Calculate early leave count
            var earlyLeaveDays = attendances.Count(a => a.IsEarlyLeave);
            
            // Calculate attendance percentage
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
                AbsentDays = absentDays,
                LateDays = lateDays,
                LeaveDays = leaveDays,
                HalfDays = halfDays,
                UnmarkedDays = unmarkedDays,
                TotalHours = totalWorkedHours,
                OvertimeHours = overtimeHours,
                AttendancePercentage = attendancePercentage,
                Attendances = attendances
            };
        }

        /// <summary>
        /// Get list of weekend days for a month
        /// </summary>
        public async Task<List<DateTime>> GetWeekendDatesInMonth(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var weekendDates = new List<DateTime>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (await IsWeekend(date))
                {
                    weekendDates.Add(date);
                }
            }

            return weekendDates;
        }

        /// <summary>
        /// Get configured weekend days
        /// </summary>
        public async Task<List<DayOfWeek>> GetConfiguredWeekendDays()
        {
            var weekendSetting = await GetActiveWeekendSetting();
            
            if (weekendSetting == null)
            {
                // Default to Friday-Saturday
                return new List<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday };
            }

            return weekendSetting.GetWeekendDays();
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
