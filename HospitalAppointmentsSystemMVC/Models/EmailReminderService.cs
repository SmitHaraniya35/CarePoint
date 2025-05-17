
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointmentsSystemMVC.Models
{
    public class EmailReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public EmailReminderService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                await CheckAndSendReminderAsync();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CheckAndSendReminderAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                var tomorrow = DateTime.Now.Date.AddDays(1);

                var upcomingAppointments = dbContext.Appointments
                    .Where(a => a.AppointmentDate.Date == tomorrow && a.Status != "cancelled")
                    .Include(a => a.Patient)
                    .ThenInclude(a => a.User)
                    .Include(a => a.Doctor)
                    .ToList();

                foreach (var appointment in upcomingAppointments)
                {
                    var patient = dbContext.Patients.FirstOrDefault(p => p.PatientId == appointment.PatientId);
                    if (patient != null && !string.IsNullOrEmpty(patient.User!.Email))
                    {
                        string email = appointment.Patient!.User!.Email;
                        string subject = "Appointment Reminder";

                        string body = $@"
                            <p>Dear {appointment.Patient.FullName},</p>
                            <p>This is a friendly reminder about your upcoming appointment with <strong>Dr. {appointment.Doctor!.FullName}</strong> on 
                            <strong>{appointment.AppointmentDate:dd MMM yyyy} at {appointment.TimeSlot}</strong>.</p>
                            <p>Please arrive 10–15 minutes early and bring any necessary documents.</p>
                            <p>If you need to reschedule, contact us at <a href='mailto:smeetce6867@gmail.com'>smeetce6867@gmail.com</a> or call us at (123) 456-7890.</p>
                            <br />
                            <p>Best regards,<br/>CarePoint Hospital</p>";

                        //await emailService.SendEmailAsync(patient.User!.Email, subject, body);
                    }
                }
            }
        }
    }
}
