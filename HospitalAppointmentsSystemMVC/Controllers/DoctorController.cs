using System.Text;
using HospitalAppointmentsSystemMVC.Models;
using HospitalAppointmentsSystemMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointmentsSystemMVC.Controllers
{
    public class DoctorController : Controller
    {
        private readonly AppDbContext _context;

        public DoctorController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Home()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var doctorId = _context.Doctors
                .Include(d => d.User)
                .Where(d => d.User.UserId == userId)
                .Select(d => d.DoctorId)
                .FirstOrDefault();

            HttpContext.Session.SetInt32("DoctorId", doctorId);

            var today = DateTime.Today;

            ViewBag.TodaysAppointmentsCount = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId && a.AppointmentDate == today && a.Status == "Pending");

            ViewBag.TotalPatientsConsulted = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Status == "Completed")
                .Select(a => a.PatientId)
                .Distinct()
                .CountAsync();

            ViewBag.UpcomingAppointmentsCount = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId && a.AppointmentDate > today && a.Status == "Pending");

            return View();
        }

        public async Task<IActionResult> ViewAppointments(string statusFilter, DateTime? dateFilter)
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            if (doctorId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var doctor = _context.Doctors
                .FirstOrDefault(d => d.DoctorId == doctorId.Value);

            if (doctor == null)
            {
                return NotFound();
            }

            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctor.DoctorId)
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
                PatientName = a.Patient.FullName,
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


        public IActionResult CancelAppointment(int id)
        {
            var appointment = _context.Appointments.FirstOrDefault(a => a.AppointmentId == id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = "Cancelled";
            appointment.CanceledBy = "Doctor";
            appointment.CancellationReason = "Doctor is not available";
            _context.SaveChanges();

            return RedirectToAction("ViewAppointments");
        }

        public IActionResult MarkMissedAppointment(int id)
        {
            var appointment = _context.Appointments.FirstOrDefault(a => a.AppointmentId == id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = "Missed";
            _context.SaveChanges();

            return RedirectToAction("ViewAppointments");
        }

        [HttpGet]
        public async Task<IActionResult> AddOrEditPrescription(int id)
        {
            int appointmentId = id;
            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            if (doctorId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctorId);

            if (appointment == null)
            {
                return NotFound();
            }

            var existingPrescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId);

            if (existingPrescription != null)
            {
                var prescription = await _context.Prescriptions
                    .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                    .FirstOrDefaultAsync(p => p.PrescriptionId == appointment.Prescription!.PrescriptionId && p.Appointment.DoctorId == doctorId);

                if (prescription == null)
                {
                    return NotFound();
                }
                var EditViewModel = new PrescriptionViewModel
                {
                    PrescriptionId = prescription.PrescriptionId,
                    AppointmentId = prescription.AppointmentId,
                    Diagnosis = prescription.Diagnosis,
                    Medicines = prescription.Medicines,
                    Notes = prescription.Notes
                };
                return View(EditViewModel);
            }

            // No prescription exists for the appointment, so create a new one
            var viewModel = new PrescriptionViewModel
            {
                AppointmentId = appointmentId
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEditPrescription(PrescriptionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            if (doctorId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId && a.DoctorId == doctorId);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = "Completed";

            // If PrescriptionId is 0, it means we are creating a new prescription
            if (model.PrescriptionId == 0)
            {
                var newPrescription = new Prescription
                {
                    AppointmentId = model.AppointmentId,
                    Diagnosis = model.Diagnosis,
                    Medicines = model.Medicines,
                    Notes = model.Notes
                };

                _context.Prescriptions.Add(newPrescription);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Edit existing prescription
                var prescriptionToUpdate = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.PrescriptionId == model.PrescriptionId);

                if (prescriptionToUpdate == null)
                {
                    return NotFound();
                }

                prescriptionToUpdate.Diagnosis = model.Diagnosis;
                prescriptionToUpdate.Medicines = model.Medicines;
                prescriptionToUpdate.Notes = model.Notes;

                _context.Prescriptions.Update(prescriptionToUpdate);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ViewAppointments");
        }

        [HttpGet]
        public IActionResult ManageDoctorProfile()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                return RedirectToAction("Index", "home");
            }
            var doctor = _context.Doctors.Include(d => d.User).FirstOrDefault(d => d.DoctorId == doctorId);

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
        public async Task<IActionResult> ManageDoctorProfile(DoctorViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DoctorId == model.DoctorId);
            if (doctor == null)
                return NotFound();

            // Update fields
            doctor.FullName = model.FullName;
            doctor.ContactNumber = model.ContactNumber;
            doctor.User.Email = model.Email;

            _context.SaveChanges();
            return RedirectToAction("Home");


        }
    
        public async Task<IActionResult> DoctorSchedules()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                return RedirectToAction("Index", "home");
            }
            var doctor = _context.Doctors.Include(d => d.User).FirstOrDefault(d => d.DoctorId == doctorId);

            if (doctor == null)
                return NotFound();

            var availability = await _context.DoctorAvailabilities
                .Include(da => da.Doctor)
                .Where(da => da.Doctor.DoctorId == doctorId)
                .Select(da => new AvailabilityViewModel
                {
                    DoctorId = da.DoctorId,
                    AvailabilityId = da.DoctorAvailabilityId,
                    Day = da.Day,
                    StartTime = da.StartTime,
                    EndTime = da.EndTime,
                })
                .ToListAsync();

            return View(availability);
        }

    }
}
