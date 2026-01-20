using Microsoft.EntityFrameworkCore;
using PayRollManagementSystem.Data;
using PayRollManagementSystem.Models;

namespace PayRollManagementSystem.Services
{
    public class LeaveApprovalService
    {
        private readonly ApplicationDbContext _context;

        public LeaveApprovalService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Auto-create attendance records for approved leave dates for SPECIFIC employee
        /// </summary>
        public async Task CreateAttendanceForApprovedLeave(int leaveId)
        {
            // Load the leave with employee information
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .ThenInclude(e => e.ShiftNavigation)
                .FirstOrDefaultAsync(l => l.LeaveId == leaveId);

            if (leave == null || leave.Status != LeaveStatus.Approved)
            {
                return;
            }

            // IMPORTANT: Only create attendance for THIS specific employee
            var currentDate = leave.StartDate;
            while (currentDate <= leave.EndDate)
            {
                // Check if attendance already exists for THIS employee on this date
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.EmployeeId == leave.EmployeeId && a.Date == currentDate);

                if (existingAttendance == null)
                {
                    // Create attendance record with OnLeave status ONLY for this employee
                    var attendance = new Attendance
                    {
                        EmployeeId = leave.EmployeeId,  // SPECIFIC employee only
                        Date = currentDate,
                        CheckInTime = leave.Employee?.ShiftNavigation?.StartTime ?? TimeSpan.Zero,
                        CheckOutTime = null,
                        Status = AttendanceStatus.OnLeave,
                        IsLate = false,
                        IsEarlyLeave = false,
                        IsHalfDay = leave.IsHalfDay && currentDate == leave.StartDate, // Only first day if half day
                        Remarks = $"On {leave.LeaveType} - Auto-marked from approved leave (Leave ID: {leave.LeaveId})",
                        CreatedAt = DateTime.Now
                    };

                    _context.Attendances.Add(attendance);
                }

                currentDate = currentDate.AddDays(1);
            }

            await _context.SaveChangesAsync();
        }
    }
}
