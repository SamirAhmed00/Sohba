using AutoMapper;
using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class StoryService : IStoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStoryDomainService _storyDomainService;

        public StoryService(IUnitOfWork unitOfWork, IMapper mapper, IStoryDomainService storyDomainService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _storyDomainService = storyDomainService;
        }

        public async Task<Result<StoryResponseDto>> CreateStoryAsync(string content, string? imageUrl, Guid userId)
        {
            var activeStories = await _unitOfWork.Stories.GetActiveStoriesAsync(userId);
            int currentStoryCount = activeStories.Count();

            var validation = _storyDomainService.CanCreateStory(userId, !string.IsNullOrEmpty(imageUrl), 10, currentStoryCount);
            if (!validation.IsSuccess)
                return Result<StoryResponseDto>.Failure(validation.Error);

            var story = new Story
            {
                UserId = userId,
                Content = content,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _unitOfWork.Stories.Add(story);
            await _unitOfWork.CompleteAsync();

            var response = _mapper.Map<StoryResponseDto>(story);
            return Result<StoryResponseDto>.Success(response);
        }

        public async Task<Result> DeleteStoryAsync(Guid storyId, Guid userId)
        {
            var story = await _unitOfWork.Stories.GetByIdAsync(storyId);

            if (story == null)
                return Result.Failure("Story not found.");

            if (story.UserId != userId)
                return Result.Failure("You are not authorized to delete this story.");

            // 3. تنفيذ الـ Soft Delete
            story.IsDeleted = true;
            _unitOfWork.Stories.Update(story);

            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result<IEnumerable<StoryResponseDto>>> GetActiveStoriesAsync(Guid userId)
        {
            var stories = await _unitOfWork.Stories.GetActiveStoriesAsync(userId);

            var response = _mapper.Map<IEnumerable<StoryResponseDto>>(stories);
            return Result<IEnumerable<StoryResponseDto>>.Success(response);
        }
    }
}
