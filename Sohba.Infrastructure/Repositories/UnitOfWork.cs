using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            // Initialize Repositories
            Users = new UserRepository(_context);
            Posts = new PostRepository(_context);
            Friendships = new FriendshipRepository(_context);
            Groups = new GroupRepository(_context);
            Stories = new StoryRepository(_context);
            Notifications = new NotificationRepository(_context);
            Reports = new ReportingRepository(_context);
            Interactions = new InteractionRepository(_context);
        }

        public IUserRepository Users { get; private set; }
        public IPostRepository Posts { get; private set; }
        public IFriendshipRepository Friendships { get; private set; }
        public IGroupRepository Groups { get; private set; }
        public IStoryRepository Stories { get; private set; }
        public INotificationRepository Notifications { get; private set; }
        public IReportingRepository Reports { get; private set; }
        public IInteractionRepository Interactions { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
