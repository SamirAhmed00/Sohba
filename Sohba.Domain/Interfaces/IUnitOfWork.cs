using System;
using System.Collections.Generic;
using System.Text;

using Sohba.Domain.Entities.AdminAggregate;

namespace Sohba.Domain.Interfaces
{
    public interface IGenericAdminLogRepository : IGenericRepository<AdminAuditLog>
    {
        Task<(IReadOnlyList<AdminAuditLog> Items, int TotalCount)> GetLogsPagedAsync(string? actionFilter, int page, int pageSize);
    }

    public interface IUnitOfWork : IDisposable
    {
        IFriendshipRepository Friendships { get; }
        IPostRepository Posts { get; }
        IInteractionRepository Interactions { get; }
        IGroupRepository Groups { get; }
        IStoryRepository Stories { get; }
        INotificationRepository Notifications { get; }
        IUserRepository Users { get; }
        IReportingRepository Reports { get; }
        IHashtagRepository Hashtags { get; }

        IPageRepository Pages { get; }

        IGenericAdminLogRepository AdminLogs { get; }

        Task<int> CompleteAsync();

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
