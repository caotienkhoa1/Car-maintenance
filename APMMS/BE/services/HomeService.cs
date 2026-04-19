using System;
using System.Linq;
using System.Threading.Tasks;
using BE.DTOs.Home;
using BE.interfaces;
using BE.models;
using BE.repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BE.services
{
    public class HomeService : IHomeService
    {
        private readonly CarMaintenanceDbContext _context;
        private readonly IServiceScheduleService _scheduleService;
        private readonly ITotalReceiptRepository _receiptRepository;

        public HomeService(
            CarMaintenanceDbContext context,
            IServiceScheduleService scheduleService,
            ITotalReceiptRepository receiptRepository)
        {
            _context = context;
            _scheduleService = scheduleService;
            _receiptRepository = receiptRepository;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(long? branchId = null)
        {
            var today = DateTime.Now.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(6);

            var stats = new DashboardStatsDto();

            // Doanh thu hôm nay
            var todayReceipts = await _receiptRepository.GetListAsync(
                statusCode: null,
                branchId: branchId,
                fromDate: today,
                toDate: today.AddDays(1).AddTicks(-1)
            );
            stats.TodayRevenue = todayReceipts.Sum(r => r.FinalAmount ?? r.Amount);

            // Doanh thu tháng này
            var monthReceipts = await _receiptRepository.GetListAsync(
                statusCode: null,
                branchId: branchId,
                fromDate: startOfMonth,
                toDate: endOfMonth
            );
            stats.MonthRevenue = monthReceipts.Sum(r => r.FinalAmount ?? r.Amount);

            // Tình trạng bảo dưỡng
            var maintenanceQuery = _context.MaintenanceTickets.AsQueryable();
            if (branchId.HasValue)
            {
                maintenanceQuery = maintenanceQuery.Where(mt => mt.BranchId == branchId.Value);
            }

            // Đang xử lý (IN_PROGRESS)
            stats.MaintenanceInProgress = await maintenanceQuery
                .CountAsync(mt => mt.StatusCode == "IN_PROGRESS");

            // Chờ xử lý (PENDING, ASSIGNED)
            stats.MaintenancePending = await maintenanceQuery
                .CountAsync(mt => mt.StatusCode == "PENDING" || mt.StatusCode == "ASSIGNED");

            // Hoàn thành hôm nay (COMPLETED trong ngày - dùng EndTime hoặc LastModifiedDate)
            stats.MaintenanceCompletedToday = await maintenanceQuery
                .Where(mt => mt.StatusCode == "COMPLETED" && 
                            ((mt.EndTime.HasValue && mt.EndTime.Value.Date == today) ||
                             (!mt.EndTime.HasValue && mt.CreatedAt.HasValue && mt.CreatedAt.Value.Date == today)))
                .CountAsync();

            // Lịch hẹn hôm nay
            stats.TodaySchedules = await _scheduleService.GetTodaySchedulesCountAsync(branchId);

            // Lịch hẹn tuần này
            var weekSchedules = await _scheduleService.GetSchedulesByDateRangeAsync(
                startOfWeek,
                endOfWeek,
                branchId
            );
            stats.WeekSchedules = weekSchedules.Count;

            // Phụ tùng xuất hôm nay (Theo nguyên tắc: Phiếu hoàn thành = Xuất kho)
            var completedTicketsTodayQuery = maintenanceQuery
                .Where(mt => mt.StatusCode == "COMPLETED" && 
                            ((mt.EndTime.HasValue && mt.EndTime.Value.Date == today) ||
                             (!mt.EndTime.HasValue && mt.CreatedAt.HasValue && mt.CreatedAt.Value.Date == today)));

            var ticketIdsToday = await completedTicketsTodayQuery.Select(mt => mt.Id).ToListAsync();

            if (ticketIdsToday.Any())
            {
                var partsToday = await _context.TicketComponents
                    .Where(tc => tc.MaintenanceTicketId.HasValue && ticketIdsToday.Contains(tc.MaintenanceTicketId.Value))
                    .ToListAsync();

                // Tính tổng số lượng (món)
                stats.TotalPartsIssuedToday = (int)partsToday.Sum(tc => tc.ActualQuantity ?? (decimal)tc.Quantity);

                // Tính tổng giá trị
                stats.TotalPartsValueToday = partsToday.Sum(tc => (tc.ActualQuantity ?? (decimal)tc.Quantity) * (tc.UnitPrice ?? 0));
            }

            return stats;
        }
    }
}

