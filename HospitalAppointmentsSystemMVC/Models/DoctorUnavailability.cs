using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
namespace HospitalAppointmentsSystemMVC.Models
{
    public class DoctorUnavailability
    {
        [Key]
        public int UnavailabilityId { get; set; }

        public int? DoctorId { get; set; }
        public int? AdminUserId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime UnavailableDate { get; set; }

        [Required]
        public string Reason { get; set; }

        // Optional: Mark who added it (Admin or Doctor)
        [Required]
        [RegularExpression(@"^(Admin|Doctor)$", ErrorMessage = "Role must be either Admin, or Doctor.")]
        public string AddedBy { get; set; }

        // Navigation property
        [ForeignKey("DoctorId")]
        [AllowNull]
        public Doctor Doctor { get; set; }

        [ForeignKey("AdminUserId")]
        [AllowNull]
        public User Admin { get; set; }
    }
}
