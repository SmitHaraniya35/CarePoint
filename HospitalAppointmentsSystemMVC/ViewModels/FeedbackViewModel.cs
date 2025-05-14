namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class FeedbackViewModel
    {
        public int FeedbackId { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }

    }
}
