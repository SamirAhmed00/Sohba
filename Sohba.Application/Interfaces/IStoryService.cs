using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Domain.Common;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IStoryService
    {
        Task<Result<StoryResponseDto>> CreateStoryAsync(StoryCreateDto storyDto, Guid userId);
        Task<Result> DeleteStoryAsync(Guid storyId, Guid userId);
        Task<Result<IEnumerable<StoryResponseDto>>> GetStoriesForFeedAsync(Guid userId);
        Task<Result<StoryResponseDto>> GetStoryByIdAsync(Guid storyId, Guid currentUserId);
        Task<Result> MarkStoryAsViewedAsync(Guid storyId, Guid userId);

        Task<Result<IEnumerable<StoryResponseDto>>> GetUserStoriesAsync(Guid userId, Guid currentUserId);

        Task<Result<(bool Added, int NewCount)>> ToggleStoryReactionAsync(Guid userId, Guid storyId, ReactionType type);
        Task<Result<IEnumerable<StoryViewerDto>>> GetStoryViewersAsync(Guid storyId, Guid currentUserId);
    }
}
