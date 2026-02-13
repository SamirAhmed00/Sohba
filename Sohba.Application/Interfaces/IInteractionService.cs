using Sohba.Domain.Common;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IInteractionService
    {
        Task<Result> ToggleReactionAsync(Guid userId, Guid postId, ReactionType type);

        Task<Result> AddCommentAsync(Guid userId, Guid postId, string content);

        Task<Result> DeleteCommentAsync(Guid userId, Guid commentId, bool isAdmin);
        Task<Result> AddReplyAsync(Guid userId, Guid commentId, string content);

        Task<Result<string>> ToggleSavePostAsync(Guid userId, Guid postId);
    }
}
