using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace HospitalAppointmentsSystemMVC.Models
{
    public class EmailService
    {
        private readonly AppDbContext _context;
        private readonly string _smtpServer = "smtp.gmail.com";     // e.g., smtp.gmail.com
        private readonly int _smtpPort = 587;                              // TLS port
        private readonly string _smtpUsername = "smeetce6867@gmail.com";
        private readonly string _smtpPassword = "dfnj kxfc vgzu isot";

        public EmailService(AppDbContext context) 
        {
            _context = context;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("HospitalAppointmentsSystem", _smtpUsername));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
