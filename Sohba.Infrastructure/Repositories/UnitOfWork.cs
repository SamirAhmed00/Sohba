using Microsoft.EntityFrameworkCore.Storage;
using Sohba.Domain.Entities.PostAggregate;
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
        private IDbContextTransaction? _transaction;

        public UnitOfWork(
            AppDbContext context,
            IUserRepository users,
            IPostRepository posts,
            IFriendshipRepository friendships,
            IGroupRepository groups,
            IStoryRepository stories,
            INotificationRepository notifications,
            IReportingRepository reports,
            IInteractionRepository interactions,
            IHashtagRepository hashtags,
            IPageRepository pages)
        {
            _context = context;
            
            // Repositories are now provided via DI
            Users = users;
            Posts = posts;
            Friendships = friendships;
            Groups = groups;
            Stories = stories;
            Notifications = notifications;
            Reports = reports;
            Interactions = interactions;
            Hashtags = hashtags;
            Pages = pages;
        }

        public IUserRepository Users { get; private set; }
        public IPostRepository Posts { get; private set; }
        public IFriendshipRepository Friendships { get; private set; }
        public IGroupRepository Groups { get; private set; }
        public IStoryRepository Stories { get; private set; }
        public INotificationRepository Notifications { get; private set; }
        public IReportingRepository Reports { get; private set; }
        public IInteractionRepository Interactions { get; private set; }
        public IPageRepository Pages { get; private set; }
        public IHashtagRepository Hashtags { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }


        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null) return;
            try
            {
                await _transaction.CommitAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null) return;
            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
