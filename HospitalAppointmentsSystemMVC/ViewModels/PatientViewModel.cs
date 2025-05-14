namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class PatientViewModel
    {
        public int PatientId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
    }
}
