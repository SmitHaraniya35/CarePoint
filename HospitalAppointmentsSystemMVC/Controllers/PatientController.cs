using System.Numerics;
using HospitalAppointmentsSystemMVC.Models;
using HospitalAppointmentsSystemMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointmentsSystemMVC.Controllers
{
    public class PatientController : Controller
    {
        private readonly AppDbContext _context;

        public PatientController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsPatientLoggedIn()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            return userId != null && role != null && role.ToLower() == "patient";
        }


        public IActionResult Home()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (!IsPatientLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            var patientId = _context.Patients
                .Include(p => p.User)
                .Where(p => p.User!.UserId == userId)
                .Select(p => p.PatientId)
                .FirstOrDefault();

            HttpContext.Session.SetInt32("PatientId", patientId);

            return View();
        }

        public async Task<IActionResult> ViewAppointments(string? statusFilter, DateTime? dateFilter)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var patient = _context.Patients
                .FirstOrDefault(d => d.PatientId == patientId.Value);

            if (patient == null)
            {
                return NotFound();
            }

            var appointments = _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patient.PatientId)
                .AsQueryable(); // Use IQueryable to build the query dynamically

            // Apply date filter if provided
            if (dateFilter.HasValue)
            {
                appointments = appointments.Where(a => a.AppointmentDate.Date == dateFilter.Value.Date);
            }

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(statusFilter))
            {
                appointments = appointments.Where(a => a.Status == statusFilter);
            }

            // Transform appointments to view model
            var viewModel = await appointments.Select(a => new AppointmentViewModel
            {
                AppointmentId = a.AppointmentId,
                DoctorName = a.Doctor!.FullName,
                AppointmentDate = a.AppointmentDate,
                TimeSlot = a.TimeSlot,
                Status = a.Status,
                CanceledBy = a.CanceledBy,
                CancellationReason = a.CancellationReason
            }).ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(viewModel);
            }

            return View(viewModel);

        }

        [HttpGet]
        public IActionResult ViewPrescription(int id)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var prescription = _context.Prescriptions
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                .Where(p =>
                    p.AppointmentId == id &&
                    p.Appointment != null &&
                    p.Appointment.Patient != null &&
                    p.Appointment.Patient.PatientId == patientId
                )
                .Select(p => new PrescriptionViewModel
                {
                    PrescriptionId = p.PrescriptionId,
                    AppointmentId = p.AppointmentId,
                    Diagnosis = p.Diagnosis,
                    Medicines = p.Medicines,
                    Notes = p.Notes
                })
                .FirstOrDefault();

            return View(prescription);
        }

        public IActionResult BookAppointment()
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Load unique specializations from doctors
            var specializations = _context.Doctors
                .Select(d => d.Specialization)
                .Distinct()
                .ToList();

            ViewBag.Specializations = specializations;

            return View(new BookAppointmentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(BookAppointmentViewModel model)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                // check what happen?
                //ViewBag.TimeSlots = new List<string> { "09:00 AM", "10:00 AM", "11:00 AM", "12:00 PM", "02:00 PM", "03:00 PM" };
                TempData["ErrorMessage"] = "Something went wrong while booking an appointment.";
                ViewBag.Specializations = _context.Doctors.Select(d => d.Specialization).Distinct().ToList();
                return View(model);
            }

            // Save appointment
            var appointment = new Appointment
            {
                PatientId = patientId.Value,
                DoctorId = model.DoctorId,
                AppointmentDate = model.AppointmentDate,
                TimeSlot = model.TimeSlot,
                Status = "Pending",
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Appointment booked successfully!";
            return RedirectToAction("ViewAppointments");
        }

        [HttpGet]
        public JsonResult GetDoctorsBySpecialization(string specialization)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return Json(null);
            }

            var doctors = _context.Doctors
                .Where(d => d.Specialization == specialization)
                .Select(d => new
                {
                    DoctorId = d.DoctorId,
                    FullName = d.FullName
                })
                .ToList();

            return Json(doctors);
        }

        [HttpGet]
        public JsonResult GetAvailableTimeSlots(int doctorId, DateTime appointmentDate)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return Json(null);
            }

            var dayOfWeek = appointmentDate.DayOfWeek;

            // Check if the doctor is unavailable on that date
            bool isUnavailable = _context.DoctorUnavailabilities
                .Any(u => u.DoctorId == doctorId && u.UnavailableDate.Date == appointmentDate.Date);

            if (isUnavailable)
                return Json(new List<string>()); // Return no slots

            // Get doctor's availability for the selected day
            var availability = _context.DoctorAvailabilities
                .FirstOrDefault(da => da.DoctorId == doctorId && da.Day == dayOfWeek);

            if (availability == null)
                return Json(new List<string>()); // No availability

            var start = availability.StartTime;
            var end = availability.EndTime;

            // Fetch existing appointments for that doctor on the selected date
            var existingAppointments = _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                            a.AppointmentDate.Date == appointmentDate.Date &&
                            a.Status.ToLower() == "pending")
                .GroupBy(a => a.TimeSlot)
                .Select(g => new { TimeSlot = g.Key, Count = g.Count() })
                .ToList();

            // Create a dictionary of slot => count
            var slotCounts = existingAppointments.ToDictionary(x => x.TimeSlot, x => x.Count);

            List<string> availableSlots = new List<string>();

            // Generate 30-minute slots
            for (var time = start; time < end; time = time.Add(TimeSpan.FromMinutes(60)))
            {
                var slot = time.ToString(@"hh\:mm");

                // Check if the slot is already booked 4 times
                if (!slotCounts.ContainsKey(time) || slotCounts[time] < 4)
                {
                    availableSlots.Add(slot);
                }
            }

            return Json(availableSlots);

        }

        [HttpGet]
        public JsonResult GetAvailableDates(int doctorId)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return Json(new
                {
                    availableDates = new List<string>(),
                    doctorUnavailable = new List<string>(),
                    hospitalClosed = new List<string>(),
                    alreadyBookedWithThisDoctor = new List<string>(),
                    patientAppointmentsWithOthers = new List<object>()
                });
            }

            var availabilities = _context.DoctorAvailabilities
                .Where(da => da.DoctorId == doctorId)
                .ToList();

            var availableDays = availabilities.Select(da => (int)da.Day).Distinct().ToList();

            var doctorUnavailabilities = _context.DoctorUnavailabilities
                .Where(du => du.DoctorId == doctorId && du.AddedBy.ToLower() != "admin")
                .Select(du => du.UnavailableDate.Date)
                .ToList();

            var hospitalClosedDates = _context.DoctorUnavailabilities
                .Where(hc => hc.AddedBy.ToLower() == "admin")
                .Select(hc => hc.UnavailableDate.Date)
                .ToList();

            var patientAppointmentsWithDoctor = _context.Appointments
                .Where(a => a.PatientId == patientId && a.DoctorId == doctorId)
                .Select(a => a.AppointmentDate.Date)
                .Distinct()
                .ToList();

            var patientAppointmentsWithOthers = _context.Appointments
                .Where(a => a.PatientId == patientId && a.DoctorId != doctorId)
                .Select(a => new { date = a.AppointmentDate.Date.ToString("yyyy-MM-dd"), a.TimeSlot })
                .ToList();

            var today = DateTime.Today;
            var range = 30;
            var availableDates = new List<string>();
            var doctorUnavailableDates = new List<string>();
            var hospitalClosedDatesStr = new List<string>();
            var alreadyBookedDates = new List<string>();

            for (int i = 0; i < range; i++)
            {
                var date = today.AddDays(i);
                var dayOfWeek = (int)date.DayOfWeek;

                var dateStr = date.ToString("yyyy-MM-dd");

                if (hospitalClosedDates.Contains(date))
                {
                    hospitalClosedDatesStr.Add(dateStr);
                }
                else if (doctorUnavailabilities.Contains(date))
                {
                    doctorUnavailableDates.Add(dateStr);
                }
                else if (patientAppointmentsWithDoctor.Contains(date))
                {
                    alreadyBookedDates.Add(dateStr);
                }
                else if (availableDays.Contains(dayOfWeek))
                {
                    availableDates.Add(dateStr);
                }
            }

            return Json(new
            {
                availableDates,
                doctorUnavailable = doctorUnavailableDates,
                hospitalClosed = hospitalClosedDatesStr,
                alreadyBookedWithThisDoctor = alreadyBookedDates,
                patientAppointmentsWithOthers
            });
        }

        [HttpGet]
        public JsonResult GetDoctorSchedules(int doctorId)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (!IsPatientLoggedIn() || patientId == null)
            {
                return Json(new
                {
                    Day = string.Empty,
                    StartTime = string.Empty,
                    EndTme = string.Empty
                });
            }

            var doctorSchedules = _context.DoctorAvailabilities
                .Include(da => da.Doctor)
                .Where(da => da.Doctor.DoctorId == doctorId)
                .Select(da => new
                {
                    Day = da.Day,
                    StartTime = da.StartTime,
                    EndTime = da.EndTime
                });

            return Json(doctorSchedules);
        }

        public IActionResult ManagePatientProfile()
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var patient = _context.Patients
                .Include(p => p.User)
                .Where(p => p.PatientId == patientId.Value)
                .Select(p => new PatientViewModel
                {
                    FullName = p.FullName,
                    Email = p.User!.Email,
                    ContactNumber = p.ContactNumber,
                })
                .FirstOrDefault();

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManagePatientProfile(PatientViewModel model)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }

                TempData["ErrorMessage"] = "Something went wrong while updating your profile.";
                return View(model);
            }

            var patient = _context.Patients
                .Include(p => p.User)
                .FirstOrDefault(p => p.PatientId == patientId.Value);

            if (patient == null)
            {
                return NotFound();
            }

            patient.FullName = model.FullName;
            patient.ContactNumber = model.ContactNumber;
            patient.User!.Email = model.Email;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Your profile updated successfully!";
            return View("Home");
        }

        public IActionResult CancelAppointment(int id)
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (!IsPatientLoggedIn() || patientId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var appointment = _context.Appointments.FirstOrDefault(a => a.AppointmentId == id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = "Cancelled";
            appointment.CanceledBy = "Patient";
            appointment.CancellationReason = "Patient is not available";
            _context.SaveChanges();

            TempData["SuccessMessage"] = "your appointment cancelled successfully.";
            return RedirectToAction("ViewAppointments");
        }
    }
}
