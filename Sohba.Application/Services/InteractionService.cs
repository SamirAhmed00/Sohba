using AutoMapper;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class InteractionService : IInteractionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IInteractionDomainService _interactionDomainService;
        private readonly IMapper _mapper;

        public InteractionService(
            IUnitOfWork unitOfWork,
            IInteractionDomainService interactionDomainService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _interactionDomainService = interactionDomainService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CommentResponseDto>> GetCommentsByPostIdAsync(Guid postId)
        {
            var comments = await _unitOfWork.Interactions.GetCommentsByPostIdAsync(postId);
            return _mapper.Map<IEnumerable<CommentResponseDto>>(comments);
        }

        public async Task<Result> AddCommentAsync(Guid userId, Guid postId, string content)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null) return Result.Failure("Post not found.");

            var canComment = _interactionDomainService.CanAddComment(userId, content, post.IsDeleted, isBlockedByOwner: false);
            if (!canComment.IsSuccess) return canComment;

            var comment = new Comment
            {
                UserId = userId,
                PostId = postId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Interactions.AddComment(comment);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> DeleteCommentAsync(Guid userId, Guid commentId, bool isAdmin)
        {
            var comment = await _unitOfWork.Interactions.GetCommentByIdAsync(commentId);
            if (comment == null) return Result.Failure("Comment not found.");

            var post = await _unitOfWork.Posts.GetByIdAsync(comment.PostId);

            var canDelete = _interactionDomainService.CanDeleteComment(userId, comment.UserId, post.UserId, isAdmin);
            if (!canDelete.IsSuccess) return canDelete;

            _unitOfWork.Interactions.RemoveComment(comment);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> AddReplyAsync(Guid userId, Guid commentId, string content)
        {
            var parentComment = await _unitOfWork.Interactions.GetCommentByIdAsync(commentId);
            if (parentComment == null) return Result.Failure("Parent comment not found.");

            var canReply = _interactionDomainService.CanReplyToComment(userId, isCommentDeleted: false, isThreadLocked: false);
            if (!canReply.IsSuccess) return canReply;

            return Result.Success();
        }

        //public async Task<Result<string>> ToggleSavePostAsync(Guid userId, Guid postId)
        //{
        //    var post = await _unitOfWork.Posts.GetByIdAsync(postId);
        //    if (post == null) return Result<string>.Failure("Post not found.");

        //    var existingSave = await _unitOfWork.Interactions.GetSavedPostAsync(userId, postId);

        //    if (existingSave != null)
        //    {
        //        _unitOfWork.Interactions.RemoveSavedPost(existingSave);
        //        await _unitOfWork.CompleteAsync();
        //        return Result<string>.Success("Post unsaved.");
        //    }
        //    else
        //    {
        //        var savedPost = new SavedPost
        //        {
        //            UserId = userId,
        //            PostId = postId,
        //            SavedAt = DateTime.UtcNow,
        //        };

        //        _unitOfWork.Interactions.AddSavedPost(savedPost);
        //        await _unitOfWork.CompleteAsync();
        //        return Result<string>.Success("Post saved.");
        //    }
        //}

        public async Task<Result> RemoveReactionAsync(Guid userId, Guid postId)
        {
            var reaction = await _unitOfWork.Interactions.GetReactionAsync(userId, postId);
            if (reaction == null)
                return Result.Failure("No reaction found");

            _unitOfWork.Interactions.RemoveReaction(reaction);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<Reaction?> GetUserReactionAsync(Guid userId, Guid postId)
        {
            return await _unitOfWork.Interactions.GetReactionAsync(userId, postId);
        }

        public async Task<Result> AddReactionAsync(Guid userId, Guid postId, ReactionType type)
        {
            var existingReaction = await _unitOfWork.Interactions.GetReactionAsync(userId, postId);

            if (existingReaction != null)
            {
                existingReaction.Type = type;
                _unitOfWork.Interactions.UpdateReaction(existingReaction);
            }
            else
            {
                var reaction = new Reaction
                {
                    UserId = userId,
                    PostId = postId,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                };
                _unitOfWork.Interactions.AddReaction(reaction);
            }

            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<int> GetReactionCountAsync(Guid postId)
        {
            return await _unitOfWork.Interactions.GetReactionCountAsync(postId);
        }

        public async Task<Result<IEnumerable<SavedPostDto>>> GetSavedPostsAsync(Guid userId)
        {
            var savedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId);
            var dtos = _mapper.Map<IEnumerable<SavedPostDto>>(savedPosts);
            return Result<IEnumerable<SavedPostDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<SavedPostDto>>> GetFavoritePostsAsync(Guid userId)
        {
            var favoritePosts = await _unitOfWork.Interactions.GetSavedPostsByUserAndTagAsync(userId, SavedTag.Favorite);
            var dtos = _mapper.Map<IEnumerable<SavedPostDto>>(favoritePosts);
            return Result<IEnumerable<SavedPostDto>>.Success(dtos);
        }

        public async Task<Result<SavedPostDto>> SavePostAsync(Guid userId, Guid postId, SavedTag tag = SavedTag.General, string? userTag = null)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null) return Result<SavedPostDto>.Failure("Post not found.");

            var existingSave = await _unitOfWork.Interactions.GetSavedPostAsync(userId, postId);

            if (existingSave != null)
            {
                existingSave.Tag = tag;
                existingSave.UserTag = userTag ?? existingSave.UserTag;
                existingSave.SavedAt = DateTime.UtcNow;

            }
            else
            {
                var savedPost = new SavedPost
                {
                    UserId = userId,
                    PostId = postId,
                    Tag = tag,
                    UserTag = userTag,
                    SavedAt = DateTime.UtcNow,
                };
                _unitOfWork.Interactions.AddSavedPost(savedPost);
            }

            await _unitOfWork.CompleteAsync();

            var resultDto = new SavedPostDto
            {
                PostId = postId,
                PostTitle = post.Title,
                Tag = tag.ToString(),
                UserTag = userTag,
                SavedAt = DateTime.UtcNow
            };
            return Result<SavedPostDto>.Success(resultDto);
        }

        public async Task<Result> RemoveSavedPostAsync(Guid userId, Guid postId)
        {
            var existingSave = await _unitOfWork.Interactions.GetSavedPostAsync(userId, postId);
            if (existingSave == null) return Result.Failure("Post is not saved.");

            _unitOfWork.Interactions.RemoveSavedPost(existingSave);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
    }
}