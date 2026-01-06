using System.Net;
using System.Net.Mail;

namespace PayRollManagementSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("EmailSettings");
                var smtpHost = smtpSettings["SmtpHost"];
                var smtpPort = int.Parse(smtpSettings["SmtpPort"] ?? "587");
                var smtpUsername = smtpSettings["SmtpUsername"];
                var smtpPassword = smtpSettings["SmtpPassword"];
                var fromEmail = smtpSettings["FromEmail"];
                var fromName = smtpSettings["FromName"];
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail!, fromName);
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                    {
                        smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                        smtpClient.EnableSsl = enableSsl;

                        await smtpClient.SendMailAsync(message);
                        _logger.LogInformation($"Email sent successfully to {toEmail}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                throw;
            }
        }

        public async Task SendLeaveRequestEmailAsync(string employeeEmail, string employeeName, string leaveType, DateTime startDate, DateTime endDate, string status)
        {
            var subject = $"Leave Request {status}";
            var body = GetLeaveRequestEmailTemplate(employeeName, leaveType, startDate, endDate, status);
            await SendEmailAsync(employeeEmail, subject, body);
        }

        public async Task SendPayslipEmailAsync(string employeeEmail, string employeeName, int month, int year, decimal netSalary)
        {
            var subject = $"Payslip for {GetMonthName(month)} {year}";
            var body = GetPayslipEmailTemplate(employeeName, month, year, netSalary);
            await SendEmailAsync(employeeEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var subject = "Reset Your Password - PayRoll Pro";
            var body = GetPasswordResetEmailTemplate(resetLink);
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            var subject = "Confirm Your Email - PayRoll Pro";
            var body = GetEmailConfirmationTemplate(confirmationLink);
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendAttendanceAlertEmailAsync(string employeeEmail, string employeeName, string alertMessage)
        {
            var subject = "Attendance Alert - PayRoll Pro";
            var body = GetAttendanceAlertEmailTemplate(employeeName, alertMessage);
            await SendEmailAsync(employeeEmail, subject, body);
        }

        public async Task SendProfileUpdateNotificationAsync(string employeeEmail, string employeeName)
        {
            var subject = "Profile Updated Successfully - PayRoll Pro";
            var body = GetProfileUpdateEmailTemplate(employeeName);
            await SendEmailAsync(employeeEmail, subject, body);
        }

        private string GetLeaveRequestEmailTemplate(string employeeName, string leaveType, DateTime startDate, DateTime endDate, string status)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2193b0; }}
        .footer {{ text-align: center; color: #5a6c7d; font-size: 12px; margin-top: 20px; }}
        .status-badge {{ display: inline-block; padding: 8px 16px; border-radius: 20px; font-weight: 600; }}
        .status-approved {{ background: #10b981; color: white; }}
        .status-rejected {{ background: #ef4444; color: white; }}
        .status-pending {{ background: #f59e0b; color: white; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>??? Leave Request {status}</h1>
        </div>
        <div class='content'>
            <p>Dear <strong>{employeeName}</strong>,</p>
            <p>Your leave request has been <span class='status-badge status-{status.ToLower()}'>{status}</span></p>
            
            <div class='info-box'>
                <p><strong>Leave Type:</strong> {leaveType}</p>
                <p><strong>Start Date:</strong> {startDate:MMMM dd, yyyy}</p>
                <p><strong>End Date:</strong> {endDate:MMMM dd, yyyy}</p>
                <p><strong>Duration:</strong> {(endDate - startDate).Days + 1} day(s)</p>
            </div>
            
            <p>If you have any questions, please contact HR.</p>
            
            <div class='footer'>
                <p>&copy; 2025 PayRoll Pro. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GetPayslipEmailTemplate(string employeeName, int month, int year, decimal netSalary)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .payslip-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #10b981; }}
        .amount {{ font-size: 32px; font-weight: bold; color: #2193b0; }}
        .footer {{ text-align: center; color: #5a6c7d; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Your Payslip is Ready</h1>
        </div>
        <div class='content'>
            <p>Dear <strong>{employeeName}</strong>,</p>
            <p>Your payslip for <strong>{GetMonthName(month)} {year}</strong> is now available.</p>
            
            <div class='payslip-box'>
                <p style='text-align: center; margin: 0; color: #5a6c7d;'>Net Salary</p>
                <p style='text-align: center;' class='amount'>?{netSalary:N2}</p>
            </div>
            
            <p>Login to your employee portal to view and download your complete payslip.</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='#' style='background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 12px 30px; text-decoration: none; border-radius: 8px; display: inline-block;'>View Payslip</a>
            </div>
            
            <div class='footer'>
                <p>&copy; 2025 PayRoll Pro. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GetPasswordResetEmailTemplate(string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .warning {{ background: #fef3cd; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f59e0b; }}
        .footer {{ text-align: center; color: #5a6c7d; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Password Reset Request</h1>
        </div>
        <div class='content'>
            <p>You recently requested to reset your password for your PayRoll Pro account.</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetLink}' style='background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 12px 30px; text-decoration: none; border-radius: 8px; display: inline-block;'>Reset Password</a>
            </div>
            
            <div class='warning'>
                <p><strong>?? Security Notice:</strong></p>
                <p>This link will expire in 24 hours. If you didn't request this password reset, please ignore this email.</p>
            </div>
            
            <div class='footer'>
                <p>&copy; 2025 PayRoll Pro. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GetEmailConfirmationTemplate(string confirmationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; color: #5a6c7d; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Confirm Your Email</h1>
        </div>
        <div class='content'>
            <p>Welcome to PayRoll Pro!</p>
            <p>Please confirm your email address by clicking the button below:</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{confirmationLink}' style='background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 12px 30px; text-decoration: none; border-radius: 8px; display: inline-block;'>Confirm Email</a>
            </div>
            
            <p>If you didn't create an account, you can safely ignore this email.</p>
            
            <div class='footer'>
                <p>&copy; 2025 PayRoll Pro. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GetAttendanceAlertEmailTemplate(string employeeName, string alertMessage)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .alert-box {{ background: #fef3cd; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f59e0b; }}
        .footer {{ text-align: center; color: #5a6c7d; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Attendance Alert</h1>
        </div>
        <div class='content'>
            <p>Dear <strong>{employeeName}</strong>,</p>
            
            <div class='alert-box'>
                <p><strong>?? Alert:</strong></p>
                <p>{alertMessage}</p>
            </div>
            
            <p>Please contact HR if you have any questions or concerns.</p>
            
            <div class='footer'>
                <p>&copy; 2025 PayRoll Pro. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GetProfileUpdateEmailTemplate(string employeeName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #6dd5ed 0%, #2193b0 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .success-box {{ background: #d1fae5; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #10b981; }}
        .footer {{ text-align: center; color: #5a6c7d; font-size: 12px; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>? Profile Updated</h1>
        </div>
        <div class='content'>
            <p>Dear <strong>{employeeName}</strong>,</p>
            
            <div class='success-box'>
                <p>Your profile has been updated successfully!</p>
                <p><strong>Date:</strong> {DateTime.Now:MMMM dd, yyyy HH:mm}</p>
            </div>
            
            <p>If you didn't make this change, please contact HR immediately.</p>
            
            <div class='footer'>
                <p>&copy; 2025 PayRoll Pro. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GetMonthName(int month)
        {
            return new DateTime(2000, month, 1).ToString("MMMM");
        }
    }
}
