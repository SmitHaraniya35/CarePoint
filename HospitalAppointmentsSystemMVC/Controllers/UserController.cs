using System.Numerics;
using BCrypt.Net;
using HospitalAppointmentsSystemMVC.Models;
using HospitalAppointmentsSystemMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointmentsSystemMVC.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public UserController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private bool IsUserLoggedIn()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            return userId != null;
        }


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check for unique email and username
                if (_context.Users.Any(u => u.Email == model.User.Email))
                {
                    ModelState.AddModelError("User.Email", "Email already in use.");
                    return View(model);
                }

                if (_context.Users.Any(u => u.Username == model.User.Username))
                {
                    ModelState.AddModelError("User.Username", "Username already in use.");
                    return View(model);
                }

                model.User.Role = "Patient"; // Set default role
                model.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.User.PasswordHash);
                _context.Users.Add(model.User);
                await _context.SaveChangesAsync(); // Save to get UserId

                model.Patient.UserId = model.User.UserId;
                _context.Patients.Add(model.Patient);
                await _context.SaveChangesAsync();

                return RedirectToAction("Login", "User");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username))
            {
                ModelState.AddModelError("username", "The username field is required.");
            }
            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("password", "The password field is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(); // Return with field-specific errors
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if(user!=null &&  user.Role!.ToLower() == "doctor")
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == user.UserId);
                
                if(doctor !=null && !doctor.IsActive)
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View();
                }

                if(doctor!=null && doctor.IsFirstLogin)
                    TempData["FirstLoginMessage"] = "Welcome! To complete your account setup, please take a moment to update your profile information.";
            }

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            // Store user info in session or cookie (simplified here)
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserRole", user.Role!);

            // Redirect based on role
            switch (user.Role!.ToLower())
            {
                case "admin":
                    return RedirectToAction("Dashboard", "Admin");
                case "doctor":
                    return RedirectToAction("Home", "Doctor");
                case "patient":
                    return RedirectToAction("Home", "Patient");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clears all session data
            return RedirectToAction("Login", "User"); // Redirect back to login page
        }

        public IActionResult ManageAccount()
        { 
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageAccount(string username, string currentPassword, string newPassword, string confirmPassword)
        {
            if (!IsUserLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                if (userId == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var user = _context.Users
                    .FirstOrDefault(u => u.UserId == userId);

                if (user == null)
                {
                    return NotFound();
                }

                // Check if the new username already exists (excluding the current user)
                bool usernameExists = _context.Users.Any(u => u.Username == username && u.UserId != userId);
                if (usernameExists)
                {
                    ModelState.AddModelError("", "Username is already taken.");
                    return View();
                }

                // Check current password
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
                if (!isPasswordValid)
                {
                    ModelState.AddModelError("", "Current password is incorrect.");
                    return View();
                }

                // Check new password confirmation
                if (newPassword != confirmPassword)
                {
                    ModelState.AddModelError("", "New password and confirmation do not match.");
                    return View();
                }

                if(user.Role!.ToLower() == "doctor")
                {
                    var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == user.UserId);
                    doctor.IsFirstLogin = false;
                }

                // Update username and password
                user.Username = username;
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Account updated successfully.";

                if(user.Role.ToLower() == "patient")
                {
                    return RedirectToAction("Home", "Patient");
                }
                if(user.Role.ToLower() == "doctor")
                {
                    return RedirectToAction("Home", "Doctor");
                }
            }

            return View();
        }
        
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel
            {
                IsOtpSent = false,
                IsOtpVerified = false
            });
        }


        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, string step)
        {
            Console.WriteLine(step);
            if (step == "SendOtp")
            {
                Console.WriteLine("Enter in Sendotp");
                Console.WriteLine(model.Email);
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {

                    //Console.WriteLine("user not found");
                    ModelState.AddModelError("", "No account found with this email.");
                    return View(model);
                }

                // Send OTP
                var otp = new Random().Next(100000, 999999).ToString();
                user.ResetOtp = otp;
                user.OtpGeneratedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var emailSubject = "Your Password Reset OTP";
                var emailBody = $"Dear {user.Email},\n\n" +
                                $"We received a request to reset your password. Please use the following OTP to reset your password:\n\n" +
                                $"OTP: {otp}\n\n" +
                                $"The OTP will expire in 10 minutes. If you did not request a password reset, please ignore this email.\n\n" +
                                $"Thank you.";


                //var _emailService = new EmailService();
                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);

                //Console.WriteLine("successfully sent");
                model.IsOtpSent = true;
                return View(new ForgotPasswordViewModel
                {
                    IsOtpSent = true,
                    Email = model.Email
                });

            }

            if (step == "VerifyOtp")
            {
                Console.WriteLine("Enter in verifyotp");
                Console.WriteLine(model.Email);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                Console.WriteLine(user);
                if(user == null)
                {
                    Console.WriteLine("user is not found");
                    ModelState.AddModelError("", "No account found with this email.");
                    return View(model);
                }
                if (user.ResetOtp != model.Otp || (DateTime.UtcNow - user.OtpGeneratedAt)?.TotalMinutes > 10)
                {
                    Console.WriteLine("Verifyotp error");
                    ModelState.AddModelError("", "Invalid or expired OTP.");
                    model.IsOtpSent = true;
                    return View(model);
                }

                model.IsOtpSent = true;
                model.IsOtpVerified = true;
                return View(model);
            }

            if (step == "ResetPassword")
            {
                Console.WriteLine("Enter in resetpassword");
                if (!ModelState.IsValid)
                {
                    model.IsOtpSent = true;
                    model.IsOtpVerified = true;
                    return View(model);
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword); // Implement your hashing method
                user.ResetOtp = null;
                user.OtpGeneratedAt = null;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Password successfully reset!";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        public IActionResult SubmitFeedback()
        {
            return RedirectToAction("Index", "Home"); 
        }
        [HttpPost]
        public IActionResult SubmitFeedback(string email, string comment, int rating)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(comment) || rating < 1 || rating > 5)
            {
                TempData["Error"] = "Please fill in all fields and provide a valid rating.";
                return RedirectToAction("Index", "Home"); // Replace with the actual view name
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                return NotFound();
            }

            var feedback = new Feedback{
                UserId = user.UserId,
                Comment = comment,
                Rating = rating
            };

            // Save to database or process feedback as needed
            _context.Feedbacks.Add(feedback);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thank you for your feedback!";
            return RedirectToAction("Index", "Home"); 
        }

    }
}
