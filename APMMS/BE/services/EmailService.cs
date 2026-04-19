using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace BE.services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["EmailSettings:SmtpUsername"] ?? "";
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "";
            _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "apmms.manage.services@gmail.com";
            _fromName = _configuration["EmailSettings:FromName"] ?? "APMMS System";
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string resetUrl)
        {
            try
            {
                var subject = "Đặt lại mật khẩu - APMMS";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>APMMS</h1>
            <p>Hệ thống Quản lý Phụ tùng & Bảo dưỡng Ô tô</p>
        </div>
        <div class='content'>
            <h2>Xin chào {userName},</h2>
            <p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
            <p>Vui lòng click vào nút bên dưới để đặt lại mật khẩu:</p>
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Đặt lại mật khẩu</a>
            </div>
            <p>Hoặc copy và dán link sau vào trình duyệt:</p>
            <p style='word-break: break-all; background: #e9ecef; padding: 10px; border-radius: 5px;'>{resetUrl}</p>
            <div class='warning'>
                <strong>⚠️ Lưu ý:</strong>
                <ul>
                    <li>Link này chỉ có hiệu lực trong <strong>24 giờ</strong></li>
                    <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
                    <li>Để bảo mật, không chia sẻ link này với bất kỳ ai</li>
                </ul>
            </div>
        </div>
        <div class='footer'>
            <p>Email này được gửi tự động từ hệ thống APMMS. Vui lòng không trả lời email này.</p>
            <p>Nếu bạn có thắc mắc, vui lòng liên hệ: <a href='mailto:apmms.manage.services@gmail.com'>apmms.manage.services@gmail.com</a></p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Nếu không có cấu hình SMTP, return true (để test)
                if (string.IsNullOrEmpty(_smtpUsername) || string.IsNullOrEmpty(_smtpPassword))
                {
                    return true; // Return true for development
                }

                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_fromEmail, _fromName);
                        message.To.Add(toEmail);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;

                        await client.SendMailAsync(message);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

