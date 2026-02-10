using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Domain.Interfaces
{
    public interface IInteractionRepository
    {
        bool HasUserReacted(Guid userId, Guid entityId);
        Task<int> GetReactionCountAsync(Guid entityId);
        Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(Guid postId);
    }
}
