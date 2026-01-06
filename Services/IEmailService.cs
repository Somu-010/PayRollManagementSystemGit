namespace PayRollManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendLeaveRequestEmailAsync(string employeeEmail, string employeeName, string leaveType, DateTime startDate, DateTime endDate, string status);
        Task SendPayslipEmailAsync(string employeeEmail, string employeeName, int month, int year, decimal netSalary);
        Task SendPasswordResetEmailAsync(string email, string resetLink);
        Task SendEmailConfirmationAsync(string email, string confirmationLink);
        Task SendAttendanceAlertEmailAsync(string employeeEmail, string employeeName, string alertMessage);
        Task SendProfileUpdateNotificationAsync(string employeeEmail, string employeeName);
    }
}
