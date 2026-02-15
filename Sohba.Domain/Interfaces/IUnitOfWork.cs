using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
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

        IPageRepository Pages { get; }


        Task<int> CompleteAsync();
    }
}
