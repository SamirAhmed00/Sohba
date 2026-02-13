using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Repositories
{
    public class InteractionRepository : IInteractionRepository
    {
        private readonly AppDbContext _context;

        public InteractionRepository(AppDbContext context)
        {
            _context = context;
        }

        // --- Reaction Implementation ---

        public async Task<Reaction?> GetReactionAsync(Guid userId, Guid postId)
        {
            // Finding a specific reaction by user and post
            return await _context.Reactions
                .FirstOrDefaultAsync(r => r.UserId == userId && r.PostId == postId);
        }

        public bool HasUserReacted(Guid userId, Guid entityId)
        {
            return _context.Reactions.Any(r => r.UserId == userId && r.PostId == entityId);
        }

        public async Task<int> GetReactionCountAsync(Guid entityId)
        {
            return await _context.Reactions.CountAsync(r => r.PostId == entityId);
        }

        public void AddReaction(Reaction reaction)
        {
            _context.Reactions.Add(reaction);
        }

        public void RemoveReaction(Reaction reaction)
        {
            _context.Reactions.Remove(reaction);
        }

        // --- Comment Implementation ---

        public async Task<Comment?> GetCommentByIdAsync(Guid commentId)
        {
            return await _context.Comments.FindAsync(commentId);
        }

        public async Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(Guid postId)
        {
            // Returning comments ordered by newest first
            return await _context.Comments
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public void AddComment(Comment comment)
        {
            _context.Comments.Add(comment);
        }

        public void RemoveComment(Comment comment)
        {
            _context.Comments.Remove(comment);
        }

        // --- SavedPost Implementation ---
        public async Task<SavedPost?> GetSavedPostAsync(Guid userId, Guid postId)
        {
            return await _context.Set<SavedPost>()
                .FirstOrDefaultAsync(sp => sp.UserId == userId && sp.PostId == postId);
        }

        public void AddSavedPost(SavedPost savedPost)
        {
            _context.Set<SavedPost>().Add(savedPost);
        }

        public void RemoveSavedPost(SavedPost savedPost)
        {
            _context.Set<SavedPost>().Remove(savedPost);
        }
    }
}
