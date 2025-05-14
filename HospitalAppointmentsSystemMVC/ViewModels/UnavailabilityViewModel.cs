namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class UnavailabilityViewModel
    {
        public int? DoctorId { get; set; }
        public string? Name { get; set; }
        public DateTime UnavailableDate { get; set; }
        public string Reason { get; set; }

        public string AddedBy { get; set; }  
    }
}
