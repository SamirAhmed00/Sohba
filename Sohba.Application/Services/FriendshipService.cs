using AutoMapper;
using Microsoft.Extensions.Logging;
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
            // Domain pre-checks: self-request, duplicate, blocked, already friends
            var alreadyFriends = await _unitOfWork.Friendships.AreFriendsAsync(senderId, receiverId);
            var hasPending = await _unitOfWork.Friendships.HasPendingRequestAsync(senderId, receiverId);
            var isBlocked = await _unitOfWork.Friendships.IsUserBlockedAsync(senderId, receiverId);

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

            // Friend entity only stores two FK GUIDs; no navigation properties needed for insert.
            // DB FK constraint enforces referential integrity — no need to fetch User entities.
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

            // Send notification to receiver
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
            var hasPending = await _unitOfWork.Friendships.HasPendingRequestAsync(senderId, receiverId);

            var alreadyFriends = await _unitOfWork.Friendships.AreFriendsAsync(senderId, receiverId);

            var decision = _domainService.CanAcceptFriendRequest(hasPending, alreadyFriends);


            if (!decision.IsSuccess)
            {
                _logger.LogWarning("Friend request accept rejected from {SenderId} to {ReceiverId}: {Reason}", senderId, receiverId, decision.Error);
                return decision;
            }

            var friendship = await _unitOfWork.Friendships.GetByUsersAsync(senderId, receiverId);

            if (friendship == null)
            {
                _logger.LogWarning("Friend request accept failed: no pending request from {SenderId} to {ReceiverId}", senderId, receiverId);
                return Result.Failure("Friend request not found.");
            }

            friendship.Status = FriendshipStatus.Accepted;
            _unitOfWork.Friendships.Update(friendship);


            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Friend request accepted: {SenderId} and {ReceiverId} are now friends", senderId, receiverId);


            // Send notification to sender
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
            var hasPending = await _unitOfWork.Friendships.HasPendingRequestAsync(senderId, receiverId);

            var decision = _domainService.CanDeclineFriendRequest(hasPending);
            if (!decision.IsSuccess)
                return decision;

            var friendship = await _unitOfWork.Friendships.GetByUsersAsync(senderId, receiverId);

            if (friendship != null)
            {
                _unitOfWork.Friendships.Delete(friendship);
                await _unitOfWork.CompleteAsync();
            }

            return Result.Success();
        }

        public async Task<Result> CancelFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            var friendship = await _unitOfWork.Friendships.GetByUsersAsync(senderId, receiverId);

            var decision = _domainService.CanCancelFriendRequest(friendship != null);
            if (!decision.IsSuccess)
                return decision;

            if (friendship != null)
            {
                _unitOfWork.Friendships.Delete(friendship);
                await _unitOfWork.CompleteAsync();
            }

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
            var reverseFriendship = await _unitOfWork.Friendships.GetByUsersAsync(friendId, userId);

            if (friendship != null)
                _unitOfWork.Friendships.Delete(friendship);

            if (reverseFriendship != null)
                _unitOfWork.Friendships.Delete(reverseFriendship);

            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result<IEnumerable<FriendDto>>> GetFriendsListAsync(Guid userId)
        {
            var friends = await _unitOfWork.Friendships.GetListByUserAsync(userId);
            //var dto = _mapper.Map<IEnumerable<FriendDto>>(friends);
            //return Result<IEnumerable<FriendDto>>.Success(dto);

            var dtos = friends.Select(f => new FriendDto
            {
                UserId = userId,
                FriendUserId = f.UserId == userId ? f.FriendUserId : f.UserId,
                FriendName = f.UserId == userId ? f.FriendUser.Name : f.User.Name,
                ProfilePictureUrl = f.UserId == userId ? f.FriendUser.ProfilePictureUrl : f.User.ProfilePictureUrl,
                Status = f.Status.ToString()
            }).ToList();
            return Result<IEnumerable<FriendDto>>.Success(dtos);
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
                FriendName = f.User.Name,
                ProfilePictureUrl = f.User.ProfilePictureUrl,
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
                FriendName = f.FriendUser.Name,
                ProfilePictureUrl = f.FriendUser.ProfilePictureUrl,
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

            var friendship = await _unitOfWork.Friendships.GetByUsersAsync(userId, targetId);
            var reverseFriendship = await _unitOfWork.Friendships.GetByUsersAsync(targetId, userId);

            if (friendship != null)
                _unitOfWork.Friendships.Delete(friendship);

            if (reverseFriendship != null)
                _unitOfWork.Friendships.Delete(reverseFriendship);

            var block = new Friend
            {
                UserId = userId,
                FriendUserId = targetId,
                Status = FriendshipStatus.Blocked,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Friendships.Add(block);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("User {UserId} blocked user {TargetId}", userId, targetId);
            return Result.Success();
        }

        public async Task<Result> UnblockUserAsync(Guid userId, Guid targetId)
        {
            var block = await _unitOfWork.Friendships.GetByUsersAsync(userId, targetId);
            var validation = _domainService.CanUnblockUser(block != null && block.Status == FriendshipStatus.Blocked);

            if (!validation.IsSuccess)
                return validation;

            _unitOfWork.Friendships.Delete(block);
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
                FriendName = f.FriendUser.Name,
                ProfilePictureUrl = f.FriendUser.ProfilePictureUrl,
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

            var blocked = await _unitOfWork.Friendships.GetBlockedUsersAsync(userId);
            var blockedIds = blocked.Select(b => b.FriendUserId).ToList();

            var excludeIds = new List<Guid> { userId };
            excludeIds.AddRange(friendIds);
            excludeIds.AddRange(sentIds);
            excludeIds.AddRange(blockedIds);

            var suggestions = await _unitOfWork.Users.GetRandomUsersAsync(excludeIds, count);

            var dtos = _mapper.Map<IEnumerable<UserResponseDto>>(suggestions);
            return Result<IEnumerable<UserResponseDto>>.Success(dtos);
        }
    }
}