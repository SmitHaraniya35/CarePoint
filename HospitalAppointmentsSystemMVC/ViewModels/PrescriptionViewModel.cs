namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class PrescriptionViewModel
    {
        public int PrescriptionId { get; set; }
        public int AppointmentId { get; set; }
        public string Diagnosis { get; set; }
        public string Medicines { get; set; }
        public string Notes { get; set; }
    }
}
