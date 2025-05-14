namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class DoctorViewModel
    {
        public int DoctorId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string Specialization { get; set; }
        public List<AvailabilityViewModel> Availabilities { get; set; } = new List<AvailabilityViewModel>();
    }
}
