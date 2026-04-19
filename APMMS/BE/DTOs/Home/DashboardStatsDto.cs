namespace BE.DTOs.Home
{
    public class DashboardStatsDto
    {
        // Doanh thu
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        
        // Tình trạng bảo dưỡng
        public int MaintenanceInProgress { get; set; }      // Đang xử lý
        public int MaintenancePending { get; set; }         // Chờ xử lý
        public int MaintenanceCompletedToday { get; set; } // Hoàn thành hôm nay
        
        // Lịch hẹn
        public int TodaySchedules { get; set; }            // Lịch hẹn hôm nay
        public int WeekSchedules { get; set; }             // Lịch hẹn tuần này

        // Phụ tùng (Mới)
        public int TotalPartsIssuedToday { get; set; }     // Tổng số phụ tùng xuất hôm nay (món)
        public decimal TotalPartsValueToday { get; set; }  // Tổng giá trị phụ tùng xuất hôm nay
    }
}

