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

        public async Task<Result<IEnumerable<PostResponseDto>>> GetSavedPostsAsync(Guid userId)
        {
            var savedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId);
            var posts = savedPosts.Select(s => s.Post).ToList();

            var dtos = await MapPostsToResponse(posts, userId);

            return Result<IEnumerable<PostResponseDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<PostResponseDto>>> GetFavoritePostsAsync(Guid userId)
        {
            var favoriteSaves = await _unitOfWork.Interactions.GetSavedPostsByUserAndTagAsync(userId, SavedTag.Favorite);
            var posts = favoriteSaves.Select(s => s.Post).ToList();

            var dtos = await MapPostsToResponse(posts, userId);

            return Result<IEnumerable<PostResponseDto>>.Success(dtos);
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

        public async Task<Result<IEnumerable<PostResponseDto>>> GetSavedPostsByTagAsync(Guid userId, SavedTag tag)
        {
            var savedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAndTagAsync(userId, tag);
            var posts = savedPosts.Select(s => s.Post).ToList();

            var dtos = await MapPostsToResponse(posts, userId);

            foreach (var dto in dtos)
            {
                dto.IsSaved = true;
                dto.IsFavorite = tag == SavedTag.Favorite;
            }

            return Result<IEnumerable<PostResponseDto>>.Success(dtos);
        }

        // Helper method to fill interaction data (Likes, Comments, ..etc)
        private async Task<IEnumerable<PostResponseDto>> MapPostsToResponse(IEnumerable<Post> posts, Guid userId)
        {
            var postList = posts.ToList();
            if (!postList.Any()) return new List<PostResponseDto>();

            var ids = postList.Select(p => p.Id).ToList();
            var counts = await _unitOfWork.Posts.GetPostsCountsAsync(ids);
            var userReactions = await _unitOfWork.Interactions.GetUserReactionsForPostsAsync(userId, ids);
            var userSavedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId);

            var reactionDict = userReactions.ToDictionary(r => r.PostId, r => r.Type.ToString());
            var savedDict = userSavedPosts.ToDictionary(s => s.PostId, s => s.Tag);

            return postList.Select(p => {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;
                dto.IsSaved = savedDict.ContainsKey(p.Id);

                if (savedDict.TryGetValue(p.Id, out var tag))
                {
                    dto.SavedTag = tag.ToString(); 
                    dto.IsFavorite = tag == SavedTag.Favorite;
                }
                dto.CurrentUserReaction = reactionDict.GetValueOrDefault(p.Id);
                return dto;
            });
        }
    }
}