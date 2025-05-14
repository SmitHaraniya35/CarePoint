namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class BookAppointmentViewModel
    {
        public string Specialization { get; set; }
        public int DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan TimeSlot { get; set; }

    }
}
