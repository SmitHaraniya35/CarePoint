namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class AvailabilityViewModel
    {
        public int AvailabilityId { get; set; }
        public int DoctorId { get; set; }
        public DayOfWeek Day { get; set; } 
        public TimeSpan StartTime { get; set; } 
        public TimeSpan EndTime { get; set; } 

    }
}
