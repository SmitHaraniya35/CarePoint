using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HospitalAppointmentsSystemMVC.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan TimeSlot { get; set; }

        [Required]
        [RegularExpression("^(Pending|Completed|Cancelled|Missed)$", ErrorMessage = "Status must be Pending, Completed, Cancelled, or Missed.")]
        public string Status { get; set; } // Pending, Completed, Cancelled, Missed

        [StringLength(100)]
        public string? CancellationReason { get; set; }

        [RegularExpression("^(Patient|Doctor|Admin)$", ErrorMessage = "CacncelledBy must be Patient, Doctor, or Admin.")]
        public string? CanceledBy { get; set; }  // Nullable: "Patient" or "Doctor"

        // Navigation
        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; }

        public Prescription? Prescription { get; set; }
    }
}
