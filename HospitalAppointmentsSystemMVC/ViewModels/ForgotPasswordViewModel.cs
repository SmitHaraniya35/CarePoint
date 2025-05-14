namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class ForgotPasswordViewModel
    {
        public string? Email { get; set; }
        public string? Otp { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }

        public bool IsOtpSent { get; set; }
        public bool IsOtpVerified { get; set; }
    }
}
