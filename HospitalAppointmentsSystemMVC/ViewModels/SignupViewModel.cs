using System.ComponentModel.DataAnnotations;
using HospitalAppointmentsSystemMVC.Models;
namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class SignupViewModel : IValidatableObject
    {
        public User User { get; set; }
        public Patient Patient { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (User.PasswordHash != ConfirmPassword)
            {
                yield return new ValidationResult(
                    "The password and confirmation password do not match.",
                    new[] { nameof(ConfirmPassword) });
            }
        }
    }
}
