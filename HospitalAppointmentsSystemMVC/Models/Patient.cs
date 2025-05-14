using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HospitalAppointmentsSystemMVC.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FullName { get; set; }

        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other.")]
        public string Gender { get; set; }

        public DateTime DateOfBirth { get; set; }

        [Phone]
        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
        public string ContactNumber { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        [ValidateNever]
        public User? User { get; set; }

        [ValidateNever]
        public ICollection<Appointment>? Appointments { get; set; }
    }
}
