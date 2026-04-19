namespace FE.viewmodels
{
    public class DashboardViewModel
    {
        public int TotalVehicles { get; set; }
        public int TotalMaintenanceRequests { get; set; }
        public int TotalParts { get; set; }
        public int TotalUsers { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
        public List<MaintenanceScheduleViewModel> UpcomingMaintenance { get; set; } = new();
    }

    public class RecentActivityViewModel
    {
        public string Activity { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string User { get; set; } = string.Empty;
    }

    public class MaintenanceScheduleViewModel
    {
        public string VehicleCode { get; set; } = string.Empty;
        public string MaintenanceType { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}


