using Aspose.Words.Drawing.Charts;

namespace HospitalAppointmentsSystemMVC.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int TotalAppointments { get; set; }
        public int UpcomingAppointmentsToday { get; set; }
        public int CancelledAppointments { get; set; }
        public int TotalFeedbacks { get; set; }

        // Chart Data (Optional)
        public List<ChartDataPoint> AppointmentsLast7Days { get; set; }
        public Dictionary<string, int> AppointmentStatus { get; set; } 
        public List<ChartDataPoint> DoctorActivity { get; set; }
        public List<ChartDataPoint> MonthlyAppointments { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public int Value { get; set; }
    }   
}
