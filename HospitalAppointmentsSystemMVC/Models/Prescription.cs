using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HospitalAppointmentsSystemMVC.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionId { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Diagnosis { get; set; }

        [Required]
        [StringLength(100)]
        public string Medicines { get; set; }

        [StringLength(100)]
        public string? Notes { get; set; }

        // Navigation
        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; }

    }
}
