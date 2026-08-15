using AutoMapper;
using Microsoft.Extensions.Logging;
using Sohba.Application.DTOs.Common;
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
        private readonly IPostDomainService _postDomainService;
        private readonly IMapper _mapper;

        private readonly INotificationService _notificationService;
        private readonly IPostService _postService;
        private readonly IUserService _userService;
        private readonly ILogger<InteractionService> _logger;

        public InteractionService(
            IUnitOfWork unitOfWork,
            IInteractionDomainService interactionDomainService,
            IMapper mapper,
            INotificationService notificationService,
            IUserService userService,
            ILogger<InteractionService> logger,
            IPostDomainService postDomainService,
            IPostService postService)
        {
            _unitOfWork = unitOfWork;
            _interactionDomainService = interactionDomainService;
            _mapper = mapper;
            _notificationService = notificationService;
            _userService = userService;
            _logger = logger;
            _postDomainService = postDomainService;
            _postService = postService;
        }

        public async Task<IEnumerable<CommentResponseDto>> GetCommentsByPostIdAsync(Guid postId, Guid currentUserId)
        {

            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeleted)
                return new List<CommentResponseDto>();

            var isFriend = await _unitOfWork.Friendships.AreFriendsAsync(currentUserId, post.UserId);
            var canView = _postDomainService.CanViewPost(currentUserId, post.UserId, post.IsPrivate, isFriend);
            if (!canView.IsSuccess)
                return new List<CommentResponseDto>();

            var comments = await _unitOfWork.Interactions.GetCommentsByPostIdAsync(postId);

            var commentDtos = _mapper.Map<IEnumerable<CommentResponseDto>>(comments).ToList();

            var replyLookup = commentDtos
                .Where(c => c.ParentCommentId.HasValue)
                .GroupBy(c => c.ParentCommentId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            CommentResponseDto AssignTree(CommentResponseDto node, int depth)
            {
                node.Depth = depth;
                node.IsAuthor = node.UserId == currentUserId;

                if (replyLookup.ContainsKey(node.Id))
                {
                    node.Replies = replyLookup[node.Id]
                        .Select(r => AssignTree(r, depth + 1))
                        .OrderByDescending(r => r.CreatedAt)
                        .ToList();
                }
                else
                {
                    node.Replies = new List<CommentResponseDto>();
                }

                node.ReplyCount = node.Depth < 4 ? node.Replies.Count : 0;
                return node;
            }

            var result = commentDtos
                .Where(c => !c.ParentCommentId.HasValue)
                .Select(c => AssignTree(c, 1))
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return result;
        }

        public async Task<Result<Guid>> AddCommentAsync(Guid userId, Guid postId, string content, Guid? parentCommentId = null)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Comment failed: post {PostId} not found", postId);
                return Result<Guid>.Failure("Post not found.");
            }

            int parentDepth = 0;
            if (parentCommentId.HasValue)
            {
                var parentComment = await _unitOfWork.Interactions.GetCommentByIdAsync(parentCommentId.Value);
                if (parentComment == null)
                    return Result<Guid>.Failure("Parent comment not found.");

                if (parentComment.PostId != postId)
                    return Result<Guid>.Failure("Parent comment does not belong to this post.");

                parentDepth = await GetCommentDepthAsync(parentCommentId.Value);
                var canReplyDepth = _interactionDomainService.CanReplyToComment(userId, false, false, parentDepth);
                if (!canReplyDepth.IsSuccess)
                    return Result<Guid>.Failure(canReplyDepth.Error);
            }


            var isBlockedByOwner = await _unitOfWork.Friendships.IsUserBlockedAsync(post.UserId, userId);
            var canComment = _interactionDomainService.CanAddComment(userId, content, post.IsDeleted, isBlockedByOwner);
            if (!canComment.IsSuccess)
            {
                _logger.LogWarning("Comment rejected for user {UserId} on post {PostId}: {Reason}", userId, postId, canComment.Error);
                return Result<Guid>.Failure(canComment.Error);
            }

            var comment = new Comment
            {
                UserId = userId,
                PostId = postId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                ParentCommentId = parentCommentId,
                Depth = parentDepth + 1
            };


            _unitOfWork.Interactions.AddComment(comment);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Comment {CommentId} added by user {UserId} on post {PostId}", comment.Id, userId, postId);


            // Send notification to post owner
            if (post.UserId != userId)
            {
                var user = await _userService.GetProfileAsync(userId);
                var userName = user.Value?.Name ?? "Someone";

                await _notificationService.CreateNotificationAsync(
                    receiverId: post.UserId,
                    message: $"{userName} commented on your post",
                    type: NotificationType.PostComment,
                    senderId: userId,
                    targetId: postId
                );
            }

            return Result<Guid>.Success(comment.Id);
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

            var canReply = _interactionDomainService.CanReplyToComment(userId, isCommentDeleted: false, isThreadLocked: false, currentDepth: parentComment.Depth);
            if (!canReply.IsSuccess) return canReply;

            // Reuse the comment-creation logic with the parent comment id.
            // This persists the reply, validates the post, and sends notifications.
            return await AddCommentAsync(userId, parentComment.PostId, content, parentCommentId: commentId);
        }

        public async Task<Result> RemoveReactionAsync(Guid userId, Guid postId)
        {
            var reaction = await _unitOfWork.Interactions.GetReactionAsync(userId, postId);
            if (reaction == null)
                return Result.Success();

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
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null)
                return Result.Failure("Post not found.");

            var isBlocked = await _unitOfWork.Friendships.IsUserBlockedAsync(post.UserId, userId);
            var canReact = _interactionDomainService.CanAddReaction(userId, post.IsDeleted, isBlocked);
            if (!canReact.IsSuccess)
                return canReact;


            var existingReaction = await _unitOfWork.Interactions.GetReactionAsync(userId, postId);

            if (existingReaction != null)
            {
                existingReaction.Type = type;
                _unitOfWork.Interactions.UpdateReaction(existingReaction);
                _logger.LogInformation("Reaction updated: user {UserId} changed reaction to {Type} on post {PostId}", userId, type, postId);
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
                _logger.LogInformation("Reaction added: user {UserId} reacted with {Type} on post {PostId}", userId, type, postId);
            }

            await _unitOfWork.CompleteAsync();

            // Send notification to post owner
            if (post != null && post.UserId != userId)
            {
                var user = await _userService.GetProfileAsync(userId);
                var userName = user.Value?.Name ?? "Someone";

                await _notificationService.CreateNotificationAsync(
                    receiverId: post.UserId,
                    message: $"{userName} reacted with {type} to your post",
                    type: NotificationType.PostLike,
                    senderId: userId,
                    targetId: postId
                );
            }

            return Result.Success();
        }

        public async Task<int> GetReactionCountAsync(Guid postId)
        {
            return await _unitOfWork.Interactions.GetReactionCountAsync(postId);
        }

        public async Task<Result<SavedPostDto?>> GetSavedPostAsync(Guid userId, Guid postId)
        {
            var saved = await _unitOfWork.Interactions.GetSavedPostAsync(userId, postId);
            if (saved == null)
                return Result<SavedPostDto?>.Success(null);

            var dto = _mapper.Map<SavedPostDto>(saved);
            return Result<SavedPostDto?>.Success(dto);
        }
        public async Task<Result<IEnumerable<PostResponseDto>>> GetSavedPostsAsync(Guid userId)
        {
            var savedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId);
            var posts = savedPosts.Select(s => s.Post).ToList();

            var mapped = await _postService.MapPostsWithInteractions(posts, userId);
            var dtos = mapped.Value ?? new List<PostResponseDto>();

            return Result<IEnumerable<PostResponseDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<PostResponseDto>>> GetFavoritePostsAsync(Guid userId)
        {
            var favoriteSaves = await _unitOfWork.Interactions.GetSavedPostsByUserAndTagAsync(userId, SavedTag.Favorite);
            var posts = favoriteSaves.Select(s => s.Post).ToList();

            var mapped = await _postService.MapPostsWithInteractions(posts, userId);
            var dtos = mapped.Value ?? new List<PostResponseDto>();

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


        // Removes the post from ALL the user's collections but KEEPS the Favorites membership.
        public async Task<Result> RemoveSavedPostsFromCollectionsAsync(Guid userId, Guid postId)
        {
            var savedPosts = (await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId))
                .Where(s => s.PostId == postId && s.Tag != SavedTag.Favorite)
                .ToList();

            if (savedPosts.Count == 0)
                return Result.Success(); // Nothing to remove from collections (still favorited).

            foreach (var savedPost in savedPosts)
            {
                _unitOfWork.Interactions.RemoveSavedPost(savedPost);
            }

            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }


        public async Task<Result<IEnumerable<PostResponseDto>>> GetSavedPostsByTagAsync(Guid userId, SavedTag tag)
        {
            var savedPosts = await _unitOfWork.Interactions.GetSavedPostsByUserAndTagAsync(userId, tag);
            var posts = savedPosts.Select(s => s.Post).ToList();

            var mapped = await _postService.MapPostsWithInteractions(posts, userId);
            var dtos = mapped.Value ?? new List<PostResponseDto>();

            foreach (var dto in dtos)
            {
                // Favorite rows are NOT "saved to a collection". The flags must stay independent.
                dto.IsSaved = tag != SavedTag.Favorite;
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
            // A post can be saved to multiple collections (e.g. a named collection AND Favorites).
            // Group by PostId and collect all tags so we don't throw on duplicate keys.
            var savedDict = userSavedPosts
                .GroupBy(s => s.PostId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Tag).ToList());

            return postList.Select(p =>
            {
                counts.TryGetValue(p.Id, out var countData);
                var dto = _mapper.Map<PostResponseDto>(p);
                dto.CommentsCount = countData.comments;
                dto.ReactionsCount = countData.reactions;

                if (savedDict.TryGetValue(p.Id, out var tags))
                {
                    // A post is "saved" only when it is in a NON-Favorite collection.
                    // Favorites alone does NOT imply Saved.
                    dto.IsSaved = tags.Any(t => t != SavedTag.Favorite);
                    dto.IsFavorite = tags.Contains(SavedTag.Favorite);
                    dto.SavedTag = dto.IsFavorite ? SavedTag.Favorite.ToString() : tags.First().ToString();
                }
                else
                {
                    dto.IsSaved = false;
                    dto.IsFavorite = false;
                }

                dto.CurrentUserReaction = reactionDict.GetValueOrDefault(p.Id);
                return dto;
            });
        }

        // Walks up ParentCommentId to compute how deep a comment is (1 = top-level comment).
        private async Task<int> GetCommentDepthAsync(Guid commentId)
        {
            var parent = await _unitOfWork.Interactions.GetCommentByIdAsync(commentId);
            return parent?.Depth ?? 0;
        }


        // ------------------------

        public async Task<Result<IEnumerable<SavedCollectionDto>>> GetUserCollectionsAsync(Guid userId)
        {
            var collections = await _unitOfWork.Interactions.GetCollectionsByUserAsync(userId);

            var dtos = collections.Select(c => new SavedCollectionDto
            {
                Id = c.Id,
                Name = c.Name,
                IsDefault = c.IsDefault,
                IsFavorites = c.IsFavorites,
                PostCount = c.SavedPosts?.Count ?? 0,
                CreatedAt = c.CreatedAt
            }).ToList();

            return Result<IEnumerable<SavedCollectionDto>>.Success(dtos);
        }

        public async Task<Result<SavedCollectionDto>> CreateCollectionAsync(Guid userId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result<SavedCollectionDto>.Failure("Collection name is required.");

            var trimmed = name.Trim();

            var existing = await _unitOfWork.Interactions.GetCollectionByNameAsync(userId, trimmed);
            if (existing != null)
                return Result<SavedCollectionDto>.Failure("A collection with this name already exists.");

            var collection = new SavedCollection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = trimmed,
                IsDefault = false,
                IsFavorites = false,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Interactions.AddCollection(collection);
            await _unitOfWork.CompleteAsync();

            var dto = new SavedCollectionDto
            {
                Id = collection.Id,
                Name = collection.Name,
                IsDefault = collection.IsDefault,
                IsFavorites = collection.IsFavorites,
                PostCount = 0,
                CreatedAt = collection.CreatedAt
            };

            return Result<SavedCollectionDto>.Success(dto);
        }

        public async Task<Result> SavePostToCollectionAsync(Guid userId, Guid postId, Guid collectionId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null) return Result.Failure("Post not found.");

            var collection = await _unitOfWork.Interactions.GetCollectionByIdAsync(collectionId);
            if (collection == null) return Result.Failure("Collection not found.");
            if (collection.UserId != userId) return Result.Failure("You do not own this collection.");

            var existing = await _unitOfWork.Interactions.GetSavedPostByCollectionAsync(userId, postId, collectionId);
            if (existing != null)
                return Result.Failure("Post is already saved to this collection.");

            var savedPost = new SavedPost
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PostId = postId,
                CollectionId = collectionId,
                Tag = SavedTag.General,
                SavedAt = DateTime.UtcNow
            };

            _unitOfWork.Interactions.AddSavedPost(savedPost);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> SavePostToFavoritesAsync(Guid userId, Guid postId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null) return Result.Failure("Post not found.");

            // Find or create the special Favorites collection.
            var favorites = (await _unitOfWork.Interactions.GetCollectionsByUserAsync(userId))
                .FirstOrDefault(c => c.IsFavorites);

            if (favorites == null)
            {
                favorites = new SavedCollection
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = "Favorites",
                    IsDefault = true,
                    IsFavorites = true,
                    CreatedAt = DateTime.UtcNow
                };
                _unitOfWork.Interactions.AddCollection(favorites);
                await _unitOfWork.CompleteAsync();
            }

            var existing = await _unitOfWork.Interactions.GetSavedPostByCollectionAsync(userId, postId, favorites.Id);
            if (existing != null)
            {
                // Toggle off: remove from favorites.
                _unitOfWork.Interactions.RemoveSavedPost(existing);
                await _unitOfWork.CompleteAsync();
                return Result.Success();
            }

            var savedPost = new SavedPost
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PostId = postId,
                CollectionId = favorites.Id,
                Tag = SavedTag.Favorite,
                SavedAt = DateTime.UtcNow
            };

            _unitOfWork.Interactions.AddSavedPost(savedPost);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result<PagedResult<SavedPostsGroupedDto>>> GetSavedPostsGroupedPagedAsync(Guid userId, int page = 1, int pageSize = 10)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var collections = (await _unitOfWork.Interactions.GetCollectionsByUserAsync(userId)).ToList();
            var allSaved = (await _unitOfWork.Interactions.GetSavedPostsByUserAsync(userId))
                .OrderByDescending(s => s.SavedAt)
                .ToList();

            var result = new List<SavedPostsGroupedDto>();

            // "All Saved" group — paginated over ALL non-Favorite rows, plus favorite rows
            // are included only if they are also saved to a collection. For the grouped page,
            // the all-saved group paginates the union of posts.
            var allPosts = allSaved
                .Where(s => s.Tag != SavedTag.Favorite || s.CollectionId != null)
                .Select(s => s.Post)
                .DistinctBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var allMapped = await MapPostsToResponse(allPosts, userId);
            result.Add(new SavedPostsGroupedDto
            {
                CollectionId = Guid.Empty,
                CollectionName = "All Saved",
                IsFavorites = false,
                Posts = allMapped.ToList()
            });

            foreach (var collection in collections)
            {
                var collectionSaves = allSaved
                    .Where(s => s.CollectionId == collection.Id)
                    .OrderByDescending(s => s.SavedAt)
                    .ToList();

                if (collectionSaves.Count == 0 && !collection.IsDefault && !collection.IsFavorites)
                    continue;

                var pagedSaves = collectionSaves
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var mapped = await MapPostsToResponse(pagedSaves.Select(s => s.Post).ToList(), userId);
                result.Add(new SavedPostsGroupedDto
                {
                    CollectionId = collection.Id,
                    CollectionName = collection.Name,
                    IsFavorites = collection.IsFavorites,
                    Posts = mapped.ToList()
                });
            }

            var totalPosts = allPosts.Count; // used only for the paging summary
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalPosts / pageSize));

            return Result<PagedResult<SavedPostsGroupedDto>>.Success(new PagedResult<SavedPostsGroupedDto>
            {
                Items = result,
                TotalCount = totalPosts,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }
    }
}