using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace HospitalAppointmentsSystemMVC.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Username must be between 3 and 50 characters.")]
        [RegularExpression(@"^(?=.*\d)[a-zA-Z0-9_]+$", ErrorMessage = "Username must contain at least one number and can only include letters, numbers, and underscores.")]
        public string Username { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+={}|\[\]:;""'<>,.?/-]).+$",
        ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
        public string PasswordHash { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; }

        [RegularExpression(@"^(Admin|Doctor|Patient)$", ErrorMessage = "Role must be either Admin, Doctor, or Patient.")]
        public string? Role { get; set; } // Admin, Doctor, Patient

        public bool IsFirstLogin { get; set; } = true;

        [StringLength(6)]
        public string? ResetOtp { get; set; }   
        
        public DateTime? OtpGeneratedAt { get; set; }

        // Navigation
        public Patient? PatientProfile { get; set; }
        public Doctor? DoctorProfile { get; set; }
    }
}
