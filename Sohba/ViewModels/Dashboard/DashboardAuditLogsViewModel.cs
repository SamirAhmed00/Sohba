using Sohba.Domain.Entities.AdminAggregate;

namespace Sohba.ViewModels.Dashboard
{
    public class DashboardAuditLogsViewModel
    {
        public List<AdminAuditLog> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string ActionFilter { get; set; } = "all";
    }

}
