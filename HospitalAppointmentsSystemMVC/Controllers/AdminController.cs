using System.Globalization;
using System.Numerics;
using HospitalAppointmentsSystemMVC.Models;
using HospitalAppointmentsSystemMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PagedList;  


namespace HospitalAppointmentsSystemMVC.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public AdminController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private bool IsAdminLoggedIn()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            return userId != null && role != null && role.ToLower() == "admin";
        }

        public IActionResult Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var today = DateTime.Today;

            var model = new AdminDashboardViewModel
            {
                TotalDoctors = _context.Doctors.Count(),
                TotalPatients = _context.Patients.Count(),
                TotalAppointments = _context.Appointments.Count(),
                UpcomingAppointmentsToday = _context.Appointments
                    .Count(a => a.AppointmentDate.Date == today && a.Status.ToLower() == "pending"),
                CancelledAppointments = _context.Appointments
                    .Count(a => a.Status.ToLower().Contains("cancelled")),
                TotalFeedbacks = _context.Feedbacks.Count(),

                // Chart Data
                AppointmentsLast7Days = GetAppointmentsLast7Days(),
                DoctorActivity = GetDoctorActivityData(),
                AppointmentStatus = GetAppointmentStatusData(),
                MonthlyAppointments = GetMonthlyAppointmentsData()
            };

            return View(model);
        }


        private List<ChartDataPoint> GetAppointmentsLast7Days()
        {
            var today = DateTime.Today;

            var data = _context.Appointments
                .Where(a => a.AppointmentDate >= today)
                .AsEnumerable() 
                .GroupBy(a => a.AppointmentDate.Date)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key.ToString("MMM dd"), 
                    Value = g.Count()
                })
                .OrderBy(dp => dp.Label)
                .ToList();

            return data;
        }

        private List<ChartDataPoint> GetDoctorActivityData()
        {
            var data = _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.Status.ToLower() != "cancelled")
                .GroupBy(a => a.Doctor!.FullName) 
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(dp => dp.Value)
                .ToList();

            return data;
        }

        private Dictionary<string, int> GetAppointmentStatusData()
        {
            var data = _context.Appointments
                .GroupBy(a => a.Status.ToLower())
                .ToDictionary(g => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(g.Key), g => g.Count());

            return data;
        }

        private List<ChartDataPoint> GetMonthlyAppointmentsData()
        {
            var currentYear = DateTime.Now.Year;

            var monthlyData = Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var count = _context.Appointments
                        .Count(a => a.AppointmentDate.Year == currentYear && a.AppointmentDate.Month == month);

                    return new ChartDataPoint
                    {
                        Label = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                        Value = count
                    };
                }).ToList();

            return monthlyData;
        }


        public async Task<IActionResult> ManageDoctors()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var doctorList = await _context.Doctors
                .Select(d => new DoctorViewModel
                {
                    DoctorId = d.DoctorId,
                    FullName = d.FullName,
                    ContactNumber = d.ContactNumber,
                    Specialization = d.Specialization,
                    Email = d.User.Email,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            return View(doctorList);
        }

        public IActionResult AddDoctor()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(DoctorViewModel model)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // Generate temporary password
                string tempPassword = GenerateTemporaryPassword();
                string tempUsername = GenerateRandomUsername();

                // Create User for the Doctor
                var doctorUser = new User
                {
                    Username = tempUsername, 
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword), // Hash the temporary password
                    Email = model.Email,
                    Role = "Doctor"
                };

                _context.Users.Add(doctorUser);
                await _context.SaveChangesAsync();

                // Create Doctor Profile
                var doctor = new Doctor
                {
                    UserId = doctorUser.UserId,
                    FullName = model.FullName,
                    Specialization = model.Specialization,
                    ContactNumber = model.ContactNumber,
                    IsActive = true
                };

                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                // Create Doctor's Availability
                foreach (var availability in model.Availabilities)
                {
                    var doctorAvailability = new DoctorAvailability
                    {
                        DoctorId = doctor.DoctorId,
                        Day = availability.Day,
                        StartTime = availability.StartTime,
                        EndTime = availability.EndTime
                    };
                    _context.DoctorAvailabilities.Add(doctorAvailability);
                }
                await _context.SaveChangesAsync();

                // Send email to doctor with the temporary password and login instructions
                var emailSubject = "Your Doctor Account - Temporary Password";
                var emailBody = $"Dear {model.FullName},\n\n" +
                                $"Your account has been created. Please log in using the following credentials:\n" +
                                $"Username: {tempUsername}\n" +
                                $"Temporary Password: {tempPassword}\n\n" +
                                $"Once logged in, you can change your password by visiting the profile settings.\n\n" +
                                $"Thank you.";

                //var _emailService = new EmailService();
                await _emailService.SendEmailAsync(model.Email, emailSubject, emailBody);

                TempData["SuccessMessage"] = "Doctor added successfully. A username and temporary password has been sent to the doctor.";
                return RedirectToAction("ManageDoctors");
            }

            TempData["ErrorMessage"] = "Something went wrong while updating the doctor.";
            return View(model);
        }
        
        private string GenerateTemporaryPassword()
        {
            const int length = 8;
            var random = new Random();

            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*()_+={}[]:;\"'<>,.?/-";

            // Ensure at least one of each required character
            var password = new List<char>
            {
                upper[random.Next(upper.Length)],
                lower[random.Next(lower.Length)],
                digits[random.Next(digits.Length)],
                special[random.Next(special.Length)]
            };

            // Fill the rest with random characters from all sets combined
            var allChars = upper + lower + digits + special;
            for (int i = password.Count; i < length; i++)
            {
                password.Add(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle to mix character types
            return new string(password.OrderBy(_ => random.Next()).ToArray());
        }

        public string GenerateRandomUsername()
        {
            var random = new Random();
            const string lettersDigitsUnderscore = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

            int length = random.Next(6, 12); // Ensure minimum length of 6, max 11 or so for variety

            // Ensure at least one digit
            var username = new List<char>
            {
                "0123456789"[random.Next(10)]
            };

            // Fill the rest
            for (int i = 1; i < length; i++)
            {
                username.Add(lettersDigitsUnderscore[random.Next(lettersDigitsUnderscore.Length)]);
            }

            // Shuffle
            return new string(username.OrderBy(_ => random.Next()).ToArray());
        }

        public IActionResult EditDoctor(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var doctor = _context.Doctors.Include(d => d.User).FirstOrDefault(d => d.DoctorId == id);

            if (doctor == null)
                return NotFound();

            var viewModel = new DoctorViewModel
            {
                DoctorId = doctor.DoctorId,
                FullName = doctor.FullName,
                Specialization = doctor.Specialization,
                ContactNumber = doctor.ContactNumber,
                Email = doctor.User.Email
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(DoctorViewModel model)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Something went wrong while updating patient details.";
                return View(model);
            }

            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.DoctorId == model.DoctorId);
            if (doctor == null)
                return NotFound();

            // Update fields
            doctor.FullName = model.FullName;
            doctor.Specialization = model.Specialization;
            doctor.ContactNumber = model.ContactNumber;
            doctor.User.Email = model.Email;

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Doctor details updated successfully.";

            return RedirectToAction("ManageDoctors");
        }

        public async Task<IActionResult> ViewDoctorAvailability(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var doctor = _context.Doctors
                .Include(d => d.User)
                .FirstOrDefault(d => d.DoctorId == id);

            if (doctor == null)
            {
                return NotFound();
            }

            var availabilities = await _context.DoctorAvailabilities
                .Where(a => a.DoctorId == id)
                .Select(a => new AvailabilityViewModel
                {
                    AvailabilityId = a.DoctorAvailabilityId,
                    DoctorId = a.DoctorId,
                    Day = a.Day,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime
                })
                .ToListAsync();

            var model = new DoctorViewModel
            {
                DoctorId = doctor.DoctorId,
                FullName = doctor.FullName,
                Email = doctor.User.Email,
                Specialization = doctor.Specialization,
                ContactNumber = doctor.ContactNumber,
                Availabilities = availabilities
            };

            return View(model);
        }

        public IActionResult EditAvailability(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            // Fetch the availability entry by its ID
            var availability = _context.DoctorAvailabilities
                .FirstOrDefault(a => a.DoctorAvailabilityId == id);

            if (availability == null)
            {
                return NotFound();
            }

            // Fetch the doctor details
            var doctor = _context.Doctors
                .Include(d => d.User)
                .FirstOrDefault(d => d.DoctorId == availability.DoctorId);

            if (doctor == null)
            {
                return NotFound();
            }

            // Create the AvailabilityViewModel from the data
            var availabilityViewModel = new AvailabilityViewModel
            {
                AvailabilityId = availability.DoctorAvailabilityId,
                DoctorId=doctor.DoctorId,
                Day = availability.Day,
                StartTime = availability.StartTime,
                EndTime = availability.EndTime
            };

            // Create the DoctorViewModel and pass it to the view
            var doctorViewModel = new DoctorViewModel
            {
                DoctorId = doctor.DoctorId,
                FullName = doctor.FullName,
                Email = doctor.User!.Email,  // Handling potential null value for User
                Specialization = doctor.Specialization,
                ContactNumber = doctor.ContactNumber,
                Availabilities = new List<AvailabilityViewModel> { availabilityViewModel } // Pass the specific availability slot
            };

            ViewBag.DoctorId = doctor.DoctorId;

            return View(doctorViewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAvailability(AvailabilityViewModel availability)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // Update existing availability
                var existing = await _context.DoctorAvailabilities
                    .FirstOrDefaultAsync(a => a.DoctorAvailabilityId == availability.AvailabilityId);

                if (existing != null)
                {
                    existing.Day = availability.Day;
                    existing.StartTime = availability.StartTime;
                    existing.EndTime = availability.EndTime;
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Doctor's availability updated successfully.";
                return RedirectToAction("ViewDoctorAvailability", new { id = existing.DoctorId });
            }

            TempData["ErrorMessage"] = "Something went wrong while updating the doctor's availability";
            return View(availability);
        }

        [HttpGet]
        public IActionResult AddAvailability(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var doctorId = id;
           
            // Get all days already available for the doctor
            var existingDays = _context.DoctorAvailabilities
                .Where(a => a.DoctorId == doctorId)
                .Select(a => a.Day)
                .ToList();

            // Get all days of the week
            var allDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>();

            // Get the days that are not already used
            var remainingDays = allDays.Except(existingDays).ToList();

            // Store the remaining days in ViewBag to populate the dropdown
            ViewBag.RemainingDays = new SelectList(remainingDays);

            var availabilityViewModel = new AvailabilityViewModel
            {
                DoctorId = (int)doctorId 
            };

            return View(availabilityViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(AvailabilityViewModel availability)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // Create a new DoctorAvailability entity
                var doctorAvailability = new DoctorAvailability
                {
                    DoctorId = availability.DoctorId,
                    Day = availability.Day,
                    StartTime = availability.StartTime,
                    EndTime = availability.EndTime
                };

                // Add the new availability to the context
                _context.DoctorAvailabilities.Add(doctorAvailability);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Doctor's availability added successfully.";
                // Redirect to the doctor availability view (or another action as needed)
                return RedirectToAction("ViewDoctorAvailability", new { id = availability.DoctorId });
            }

            TempData["ErrorMessage"] = "Something went wrong while adding availability of the doctor.";
            return View(availability);
        
        }

        public async Task<IActionResult> DeleteAvailability(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var availability = await _context.DoctorAvailabilities.FirstOrDefaultAsync(a => a.DoctorAvailabilityId == id);

            if (availability != null)
            {
                _context.DoctorAvailabilities.Remove(availability);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Doctor's avilability deleted successfully.";
                return RedirectToAction("ViewDoctorAvailability", new { id = availability.DoctorId });
            }

            TempData["ErrorMessage"] = "Something went wrong while deleting doctor's availability";
            return RedirectToAction("ViewDoctorAvailability", new { id = availability.DoctorId });
        }

        public async Task<IActionResult> InActivateDoctor(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == id);

            if (doctor != null)
            {
                doctor.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Doctor inactivated successfully.";
                return RedirectToAction("ManageDoctors");
            }

            TempData["ErrorMessage"] = "Something went wrong while inactivating the doctor.";
            return RedirectToAction("ManageDoctors");
        }

        public async Task<IActionResult> ActivateDoctor(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == id);

            if (doctor != null)
            {
                doctor.IsActive = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Doctor activated successfully.";
                return RedirectToAction("ManageDoctors");
            }

            TempData["ErrorMessage"] = "Something went wrong while activating the doctor.";
            return RedirectToAction("ManageDoctors");
        }

        public async Task<IActionResult> ManagePatients()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var patientList = await _context.Patients
                .Select(p => new PatientViewModel
                {
                    PatientId = p.PatientId,
                    FullName = p.FullName,
                    Email = p.User!.Email,
                    ContactNumber = p.ContactNumber,
                    Gender = p.Gender,
                    DateOfBirth = p.DateOfBirth
                })
                .ToListAsync();

            return View(patientList);

        }

        public IActionResult EditPatient(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var patient = _context.Patients.Include(p => p.User).FirstOrDefault(p => p.PatientId == id);

            if (patient == null)
                return NotFound();

            var viewModel = new PatientViewModel
            {
                PatientId = patient.PatientId,
                FullName = patient.FullName,
                Email = patient.User!.Email,
                ContactNumber = patient.ContactNumber,
                Gender = patient.Gender,
                DateOfBirth= patient.DateOfBirth
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPatient(PatientViewModel model)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Something went wrong while updating patient details.";
                return View(model);
            }

            var patient = _context.Patients.Include(p => p.User).FirstOrDefault(p => p.PatientId == model.PatientId);
            if (patient == null)
                return NotFound();

            // Update fields
            patient.FullName = model.FullName;
            patient.ContactNumber = model.ContactNumber;
            patient.Gender = model.Gender!;
            patient.DateOfBirth = model.DateOfBirth;

            // Update User (email only in this case)
            if (patient.User != null)
            {
                patient.User.Email = model.Email;
            }

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Patient details updated successfully.";
            return RedirectToAction("ManagePatients");
        }

        public async Task<IActionResult> ViewAppointments(int? id, string role)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var appointments = new List<AppointmentViewModel> { };
            if (id == null && role == "admin")
            {
                appointments = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Select(a => new AppointmentViewModel
                {
                    AppointmentId = a.AppointmentId,
                    PatientName = a.Patient != null ? a.Patient.FullName : "Unknown",
                    DoctorName = a.Doctor != null ? a.Doctor.FullName : "Unknown",
                    AppointmentDate = a.AppointmentDate,
                    TimeSlot = a.TimeSlot,
                    Status = a.Status ?? "N/A",
                    CanceledBy = a.CanceledBy ?? "N/A",
                    CancellationReason = a.CancellationReason ?? "N/A"
                })
                .ToList();
                ViewBag.Role = "admin";
            }
            else if (role == "doctor")
            {
                appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == id)
                    .Select(a => new AppointmentViewModel
                    {
                        AppointmentId = a.AppointmentId,
                        PatientName = a.Patient!.FullName,
                        AppointmentDate = a.AppointmentDate,
                        TimeSlot = a.TimeSlot,
                        Status = a.Status,
                        CanceledBy = a.CanceledBy ?? "N/A",
                        CancellationReason = a.CancellationReason ?? "N/A"
                    })
                    .ToListAsync();
                ViewBag.Role = "doctor";
            }
            else if (role == "patient")
            {
                appointments = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .Where(a => a.PatientId == id)
                    .Select(a => new AppointmentViewModel
                    {
                        AppointmentId = a.AppointmentId,
                        DoctorName = a.Doctor!.FullName,
                        AppointmentDate = a.AppointmentDate,
                        TimeSlot = a.TimeSlot,
                        Status = a.Status,
                        CanceledBy = a.CanceledBy ?? "N/A",
                        CancellationReason = a.CancellationReason ?? "N/A"
                    })
                    .ToListAsync();
                ViewBag.Role = "patient";
            }

            if (appointments == null || !appointments.Any())
            {
                ViewBag.Message = "No appointments found for this patient.";
            }

            return View(appointments);
        }

        public IActionResult DeleteAppointment(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var appointment = _context.Appointments.Find(id);
            if (appointment == null)
            {
                return NotFound();
            }

            _context.Appointments.Remove(appointment);
            _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Appointment deleted successfully.";
            return RedirectToAction("ViewAppointments", new { role = "admin"});
        }

        public async Task<IActionResult> ViewFeedbacks()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var feedbacks = await _context.Feedbacks
                .Include(f => f.User)
                .Select(f => new FeedbackViewModel
                {
                    FeedbackId = f.FeedbackId,
                    UserEmail = f.User.Email,
                    Comment = f.Comment,
                    Rating = f.Rating
                })
                .ToListAsync();

            foreach(var feedback in feedbacks)
            {
                feedback.UserName = _context.Patients
                    .Include(p => p.User)
                    .Where(p => p.User!.Email == feedback.UserEmail)
                    .Select(p => p.FullName)
                    .FirstOrDefault()
                    ?? _context.Doctors
                    .Include(d => d.User)
                    .Where(d => d.User!.Email == feedback.UserEmail)
                    .Select(d => d.FullName)
                    .FirstOrDefault();
            }

            return View(feedbacks);
        }

        public async Task<IActionResult> ViewUnavailability()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var unavailabilities = await _context.DoctorUnavailabilities
                .Include(u => u.Doctor)
                .Include(u => u.Admin)
                .Select(u => new UnavailabilityViewModel{
                    UnavailableDate = u.UnavailableDate,
                    Reason = u.Reason,
                    AddedBy = u.AddedBy,
                    Name = (u.DoctorId == null ? "Admin" : u.Doctor.FullName)
                })
                .ToListAsync();

            return View(unavailabilities);
        }

        [HttpGet]
        public IActionResult ReportUnavailability()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (userId == null || (role != null && role.ToLower() == "patient"))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ReportUnavailability(UnavailabilityViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (userId == null || (role != null && role.ToLower() == "patient"))
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Something went wrong while reporting unavailability.";
                return View(model);
            }

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

            if (user == null)
                return NotFound();

            var unavailability = new DoctorUnavailability
            {
                UnavailableDate = model.UnavailableDate.Date,
                Reason = model.Reason,
                AddedBy = user.Role! 
            };

            if (user.Role!.ToLower() == "doctor")
            {
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null)
                {
                    return NotFound();
                }

                unavailability.DoctorId = doctor.DoctorId;
                model.Reason = "Doctor is not available";
            }
            else if (user.Role.ToLower() == "admin")
            {
                unavailability.AdminUserId = user.UserId;
            }

            _context.DoctorUnavailabilities.Add(unavailability);
            await _context.SaveChangesAsync();

            model.AddedBy = user.Role;
            CancelAllAppointments(model);
            
            if(user.Role.ToLower() == "doctor")
            {
                return RedirectToAction("DoctorSchedules", "Doctor");
            }
            return RedirectToAction("ViewUnavailability");
        }

        public void CancelAllAppointments(UnavailabilityViewModel model)
        {
            var appointmentsToCancel = model.DoctorId.HasValue
                ? _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Doctor)
                        .ThenInclude(d => d.User)
                    .Where(a => a.DoctorId == model.DoctorId.Value && a.AppointmentDate.Date == model.UnavailableDate.Date)
                    .ToList()
                : _context.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Include(a => a.Doctor)
                        .ThenInclude(d => d.User)
                    .Where(a => a.AppointmentDate.Date == model.UnavailableDate.Date)
                    .ToList();


            // Group by Patient to send a single email with only the affected appointments
            var groupedByPatient = appointmentsToCancel
                .GroupBy(a => a.Patient.PatientId);

            foreach (var group in groupedByPatient)
            {
                var patient = group.First().Patient;

                // Construct email
                string subject = "Appointment Cancellation Notice";
                string body = $"Dear {patient.FullName},\n\n";

                if (model.AddedBy.ToLower() == "doctor")
                {
                    body += $"The following appointment(s) with your doctor have been cancelled because the doctor is unavailable on {model.UnavailableDate:dd MMM yyyy}:\n";
                }
                else
                {
                    body += $"The hospital has cancelled the following appointment(s) on {model.UnavailableDate:dd MMM yyyy}:\nReason: {model.Reason}\n";
                }

                foreach (var appointment in group)
                {
                    appointment.Status = "Cancelled";
                    appointment.CancellationReason = model.Reason;
                    appointment.CanceledBy = model.AddedBy;

                    body += $"- Doctor: {appointment.Doctor!.FullName}, Time: {appointment.TimeSlot}\n";
                }

                body += "\nWe apologize for the inconvenience.\nThank you.";

                // Send the email
                _emailService.SendEmailAsync(patient.User.Email, subject, body);
            }

            // Notify affected doctors if cancelled by admin
            if (model.AddedBy.ToLower() != "doctor")
            {
                var groupedByDoctor = appointmentsToCancel
                    .GroupBy(a => a.Doctor.DoctorId);

                foreach (var group in groupedByDoctor)
                {
                    var doctor = group.First().Doctor;

                    string doctorSubject = "Appointment Cancellations on " + model.UnavailableDate.ToString("dd MMM yyyy");
                    string doctorBody = $"Dear Dr. {doctor.FullName},\n\n" +
                                        $"Your appointments on {model.UnavailableDate:dd MMM yyyy} have been cancelled due to:\n{model.Reason}\n\n";

                    foreach (var appt in group)
                    {
                        doctorBody += $"- Patient: {appt.Patient.FullName}, Time: {appt.TimeSlot}\n";
                    }

                    doctorBody += "\nRegards,\nHospital Admin";

                    _emailService.SendEmailAsync(doctor.User!.Email, doctorSubject, doctorBody);
                }
            }

            _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Unavailability reported successfully and Affected appointments cancelled and notifications sent.";
        }
    }
}
