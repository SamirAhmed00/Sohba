using AutoMapper;
using Microsoft.Extensions.Logging;
using Sohba.Application.DTOs.Common;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Sohba.Application.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipDomainService _domainService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        private readonly ILogger<FriendshipService> _logger;

        public FriendshipService(
            IFriendshipDomainService domainService,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IUserService userService,
            ILogger<FriendshipService> logger)
        {
            _domainService = domainService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _userService = userService;
            _logger = logger;
        }

        // Friend Requests
        public async Task<Result> SendFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            var alreadyFriends = await _unitOfWork.Friendships.AreFriendsAsync(senderId, receiverId);
            var hasPending = await _unitOfWork.Friendships.HasPendingRequestAsync(senderId, receiverId);
            var isBlocked = await _unitOfWork.Friendships.IsBlockedEitherDirectionAsync(senderId, receiverId);

            var decision = _domainService.CanSendFriendRequest(
                senderId,
                receiverId,
                alreadyFriends,
                hasPending,
                isBlocked
            );

            if (!decision.IsSuccess)
            {
                _logger.LogWarning("Friend request rejected from {SenderId} to {ReceiverId}: {Reason}", senderId, receiverId, decision.Error);
                return decision;
            }

            var friendRequest = new Friend
            {
                UserId = senderId,
                FriendUserId = receiverId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };

            _unitOfWork.Friendships.Add(friendRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Friend request sent from {SenderId} to {ReceiverId}", senderId, receiverId);

            var user = await _userService.GetProfileAsync(senderId);
            var userName = user.Value?.Name ?? "Someone";

            await _notificationService.CreateNotificationAsync(
                receiverId: receiverId,
                message: $"{userName} sent you a friend request",
                type: NotificationType.FriendRequest,
                senderId: senderId
            );

            return Result.Success();
        }

        public async Task<Result> AcceptFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            // Strict directional lookup: receiverId MUST be the recipient of the pending request
            var pendingRequest = await _unitOfWork.Friendships.GetDirectAsync(senderId, receiverId);
            var alreadyFriends = await _unitOfWork.Friendships.AreFriendsAsync(senderId, receiverId);

            var decision = _domainService.CanAcceptFriendRequest(
                pendingRequest != null && pendingRequest.Status == FriendshipStatus.Pending,
                alreadyFriends);

            if (!decision.IsSuccess || pendingRequest == null || pendingRequest.Status != FriendshipStatus.Pending)
            {
                _logger.LogWarning("Friend request accept rejected: user {ReceiverId} cannot accept request from {SenderId}", receiverId, senderId);
                return Result.Failure("Pending friend request not found.");
            }

            pendingRequest.Status = FriendshipStatus.Accepted;
            _unitOfWork.Friendships.Update(pendingRequest);

            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Friend request accepted: {SenderId} and {ReceiverId} are now friends", senderId, receiverId);

            var user = await _userService.GetProfileAsync(receiverId);
            var userName = user.Value?.Name ?? "Someone";

            await _notificationService.CreateNotificationAsync(
                receiverId: senderId,
                message: $"{userName} accepted your friend request",
                type: NotificationType.FriendRequest,
                senderId: receiverId
            );

            return Result.Success();
        }

        public async Task<Result> RejectFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            // Strict directional lookup: receiverId MUST be the recipient
            var pendingRequest = await _unitOfWork.Friendships.GetDirectAsync(senderId, receiverId);

            var decision = _domainService.CanDeclineFriendRequest(
                pendingRequest != null && pendingRequest.Status == FriendshipStatus.Pending);

            if (!decision.IsSuccess || pendingRequest == null || pendingRequest.Status != FriendshipStatus.Pending)
            {
                return Result.Failure("Pending friend request not found.");
            }

            _unitOfWork.Friendships.Delete(pendingRequest);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> CancelFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            // Strict directional lookup: senderId MUST be the creator of the pending request
            var pendingRequest = await _unitOfWork.Friendships.GetDirectAsync(senderId, receiverId);

            var decision = _domainService.CanCancelFriendRequest(
                pendingRequest != null && pendingRequest.Status == FriendshipStatus.Pending);

            if (!decision.IsSuccess || pendingRequest == null || pendingRequest.Status != FriendshipStatus.Pending)
            {
                return Result.Failure("No pending sent request found to cancel.");
            }

            _unitOfWork.Friendships.Delete(pendingRequest);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        // Friends Management
        public async Task<Result> UnfriendAsync(Guid userId, Guid friendId)
        {
            var alreadyFriends = await _unitOfWork.Friendships.AreFriendsAsync(userId, friendId);

            var decision = _domainService.CanRemoveFriend(alreadyFriends);
            if (!decision.IsSuccess)
                return decision;

            var friendship = await _unitOfWork.Friendships.GetByUsersAsync(userId, friendId);

            if (friendship != null && friendship.Status == FriendshipStatus.Accepted)
            {
                _unitOfWork.Friendships.Delete(friendship);
                await _unitOfWork.CompleteAsync();
            }

            return Result.Success();
        }

        public async Task<Result<IEnumerable<FriendDto>>> GetFriendsListAsync(Guid userId)
        {
            var friends = await _unitOfWork.Friendships.GetListByUserAsync(userId);

            var dtos = friends.Select(f => new FriendDto
            {
                UserId = userId,
                FriendUserId = f.UserId == userId ? f.FriendUserId : f.UserId,
                FriendName = f.UserId == userId ? (f.FriendUser != null ? f.FriendUser.Name : "Unknown") : (f.User != null ? f.User.Name : "Unknown"),
                ProfilePictureUrl = f.UserId == userId ? f.FriendUser?.ProfilePictureUrl : f.User?.ProfilePictureUrl,
                Status = f.Status.ToString()
            }).ToList();

            return Result<IEnumerable<FriendDto>>.Success(dtos);
        }

        public async Task<Result<PagedResult<FriendDto>>> GetFriendsListPagedAsync(Guid userId, string? search = null, int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 12;

            var friends = await _unitOfWork.Friendships.GetListByUserAsync(userId);

            var dtosQuery = friends.Select(f => new FriendDto
            {
                UserId = userId,
                FriendUserId = f.UserId == userId ? f.FriendUserId : f.UserId,
                FriendName = f.UserId == userId ? (f.FriendUser != null ? f.FriendUser.Name : "Unknown") : (f.User != null ? f.User.Name : "Unknown"),
                ProfilePictureUrl = f.UserId == userId ? f.FriendUser?.ProfilePictureUrl : f.User?.ProfilePictureUrl,
                Status = f.Status.ToString()
            });

            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim();
                dtosQuery = dtosQuery.Where(f => f.FriendName != null && f.FriendName.Contains(cleanSearch, StringComparison.OrdinalIgnoreCase));
            }

            var dtosList = dtosQuery.ToList();
            var totalCount = dtosList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedItems = dtosList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PagedResult<FriendDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return Result<PagedResult<FriendDto>>.Success(result);
        }

        public async Task<bool> AreFriendsAsync(Guid userId, Guid friendId)
        {
            return await _unitOfWork.Friendships.AreFriendsAsync(userId, friendId);
        }


        public async Task<bool> HasPendingRequestAsync(Guid senderId, Guid receiverId)
        {
            return await _unitOfWork.Friendships.HasPendingRequestAsync(senderId, receiverId);
        }

        public async Task<Result<IEnumerable<FriendDto>>> GetPendingRequestsAsync(Guid userId)
        {
            var requests = await _unitOfWork.Friendships.GetPendingRequestsAsync(userId);
            var dtos = requests.Select(f => new FriendDto
            {
                UserId = userId,
                FriendUserId = f.UserId,
                FriendName = f.User != null ? f.User.Name : "Unknown",
                ProfilePictureUrl = f.User?.ProfilePictureUrl,
                Status = f.Status.ToString()
            }).ToList();

            return Result<IEnumerable<FriendDto>>.Success(dtos);
        }

        public async Task<Result<IEnumerable<FriendDto>>> GetSentRequestsAsync(Guid userId)
        {
            var requests = await _unitOfWork.Friendships.GetSentRequestsAsync(userId);
            var dtos = requests.Select(f => new FriendDto
            {
                UserId = userId,
                FriendUserId = f.FriendUserId,
                FriendName = f.FriendUser != null ? f.FriendUser.Name : "Unknown",
                ReceiverName = f.FriendUser != null ? f.FriendUser.Name : "Unknown",
                ProfilePictureUrl = f.FriendUser?.ProfilePictureUrl,
                Status = f.Status.ToString()
            }).ToList();

            return Result<IEnumerable<FriendDto>>.Success(dtos);
        }

        public async Task<Result<int>> GetPendingRequestsCountAsync(Guid userId)
        {
            var count = await _unitOfWork.Friendships.GetPendingRequestsCountAsync(userId);
            return Result<int>.Success(count);
        }

        // Blocking
        public async Task<Result> BlockUserAsync(Guid userId, Guid targetId)
        {
            var alreadyBlocked = await _unitOfWork.Friendships.IsUserBlockedAsync(userId, targetId);
            var validation = _domainService.CanBlockUser(userId, targetId, alreadyBlocked);

            if (!validation.IsSuccess)
            {
                _logger.LogWarning("Block action rejected: user {UserId} cannot block {TargetId}: {Reason}", userId, targetId, validation.Error);
                return validation;
            }

            var existingRelationship = await _unitOfWork.Friendships.GetByUsersAsync(userId, targetId);

            if (existingRelationship != null)
            {
                if (existingRelationship.UserId == userId && existingRelationship.FriendUserId == targetId)
                {
                    existingRelationship.Status = FriendshipStatus.Blocked;
                    existingRelationship.CreatedAt = DateTime.UtcNow;
                    _unitOfWork.Friendships.Update(existingRelationship);
                }
                else
                {
                    _unitOfWork.Friendships.Delete(existingRelationship);
                    var blockRecord = new Friend
                    {
                        UserId = userId,
                        FriendUserId = targetId,
                        Status = FriendshipStatus.Blocked,
                        CreatedAt = DateTime.UtcNow
                    };
                    _unitOfWork.Friendships.Add(blockRecord);
                }
            }
            else
            {
                var blockRecord = new Friend
                {
                    UserId = userId,
                    FriendUserId = targetId,
                    Status = FriendshipStatus.Blocked,
                    CreatedAt = DateTime.UtcNow
                };
                _unitOfWork.Friendships.Add(blockRecord);
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("User {UserId} blocked user {TargetId}", userId, targetId);
            return Result.Success();
        }

        public async Task<Result> UnblockUserAsync(Guid userId, Guid targetId)
        {
            // Strict directional lookup: userId MUST be the blocker
            var directBlock = await _unitOfWork.Friendships.GetDirectAsync(userId, targetId);
            var validation = _domainService.CanUnblockUser(directBlock != null && directBlock.Status == FriendshipStatus.Blocked);

            if (!validation.IsSuccess || directBlock == null || directBlock.Status != FriendshipStatus.Blocked)
            {
                return Result.Failure("User is not blocked.");
            }

            _unitOfWork.Friendships.Delete(directBlock);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result<IEnumerable<FriendDto>>> GetBlockedUsersAsync(Guid userId)
        {
            var blocked = await _unitOfWork.Friendships.GetBlockedUsersAsync(userId);
            var dtos = blocked.Select(f => new FriendDto
            {
                UserId = userId,
                FriendUserId = f.FriendUserId,
                FriendName = f.FriendUser != null ? f.FriendUser.Name : "Unknown",
                ProfilePictureUrl = f.FriendUser?.ProfilePictureUrl,
                Status = f.Status.ToString()
            }).ToList();

            return Result<IEnumerable<FriendDto>>.Success(dtos);
        }

        public async Task<bool> IsBlockedAsync(Guid userId, Guid targetId)
        {
            return await _unitOfWork.Friendships.IsUserBlockedAsync(userId, targetId);
        }

        // Suggestions
        public async Task<Result<IEnumerable<UserResponseDto>>> GetFriendSuggestionsAsync(Guid userId, int count = 10)
        {
            var friends = await _unitOfWork.Friendships.GetListByUserAsync(userId);
            var friendIds = friends.Select(f => f.UserId == userId ? f.FriendUserId : f.UserId).ToList();

            var sentRequests = await _unitOfWork.Friendships.GetSentRequestsAsync(userId);
            var sentIds = sentRequests.Select(r => r.FriendUserId).ToList();

            var pendingIncoming = await _unitOfWork.Friendships.GetPendingRequestsAsync(userId);
            var pendingIncomingIds = pendingIncoming.Select(r => r.UserId).ToList();

            var blocked = await _unitOfWork.Friendships.GetBlockedUsersAsync(userId);
            var blockedIds = blocked.Select(b => b.FriendUserId).ToList();
            var blockedByIds = await _unitOfWork.Friendships.GetBlockedByAsync(userId);

            var excludeIds = new List<Guid> { userId };
            excludeIds.AddRange(friendIds);
            excludeIds.AddRange(sentIds);
            excludeIds.AddRange(pendingIncomingIds);
            excludeIds.AddRange(blockedIds);
            excludeIds.AddRange(blockedByIds);

            var suggestions = await _unitOfWork.Users.GetRandomUsersAsync(excludeIds, count);

            var dtos = _mapper.Map<IEnumerable<UserResponseDto>>(suggestions);
            return Result<IEnumerable<UserResponseDto>>.Success(dtos);
        }
    }

}