using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalAppointmentsSystemMVC.Models
{
    public class DoctorAvailability
    {
        [Key]
        public int DoctorAvailabilityId { get; set; }

        public int DoctorId { get; set; }

        [Required]
        public DayOfWeek Day { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        // Navigation
        [ForeignKey("DoctorId")]
        public Doctor Doctor { get; set; }
    }
}
