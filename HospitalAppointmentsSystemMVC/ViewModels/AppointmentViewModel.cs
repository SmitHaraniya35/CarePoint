namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class AppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan TimeSlot { get; set; }
        public string Status { get; set; }
        public string? CanceledBy { get; set; }
        public string? CancellationReason { get; set; }
    }
}
