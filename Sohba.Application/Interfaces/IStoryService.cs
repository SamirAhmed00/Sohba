using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Interfaces
{
    public interface IStoryService
    {
        Task<Result<StoryResponseDto>> CreateStoryAsync(string content, string? imageUrl, Guid userId);
        Task<Result> DeleteStoryAsync(Guid storyId, Guid userId);
        Task<Result<IEnumerable<StoryResponseDto>>> GetActiveStoriesAsync(Guid userId);
    }
}
