using BE.DTOs.Home;

namespace BE.interfaces
{
    public interface IHomeService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync(long? branchId = null);
    }
}

