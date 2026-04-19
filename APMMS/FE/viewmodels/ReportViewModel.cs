namespace FE.viewmodels
{
    public class ReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public List<ReportDataViewModel> Data { get; set; } = new();
        public ReportSummaryViewModel Summary { get; set; } = new();
    }

    public class ReportDataViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int Count { get; set; }
        public DateTime Date { get; set; }
    }

    public class ReportSummaryViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalMaintenance { get; set; }
        public int TotalVehicles { get; set; }
        public int TotalParts { get; set; }
        public decimal AverageCost { get; set; }
    }

    public class MaintenanceReportViewModel
    {
        public string VehicleCode { get; set; } = string.Empty;
        public string MaintenanceType { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Cost { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Technician { get; set; } = string.Empty;
    }

    public class PartsReportViewModel
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public int QuantityUsed { get; set; }
        public decimal TotalCost { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}


