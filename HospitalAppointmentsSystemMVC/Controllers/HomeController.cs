using System.Diagnostics;
using HospitalAppointmentsSystemMVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointmentsSystemMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult BookAppointment()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var patientId = HttpContext.Session.GetInt32("PatientId");

            if (userId != null && patientId != null)
            {
                return RedirectToAction("BookAppointment", "Patient");
            }

            return RedirectToAction("login", "User");
        }
    }   
}
