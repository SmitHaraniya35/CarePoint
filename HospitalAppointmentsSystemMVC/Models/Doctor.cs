using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HospitalAppointmentsSystemMVC.Models
{
    public class Doctor
    {
        [Key]
        public int DoctorId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FullName { get; set; }

        [Required]
        [StringLength(50)]
        public string Specialization { get; set; }

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
        [Phone]
        public string ContactNumber { get; set; }

        public bool IsActive { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
    }
}
