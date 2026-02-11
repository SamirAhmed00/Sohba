using AutoMapper;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Common;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class SocialService : ISocialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFriendshipDomainService _friendshipDomainService;

        public SocialService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFriendshipDomainService friendshipDomainService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _friendshipDomainService = friendshipDomainService;
        }

        public async Task<Result> SendFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            var alreadyFriends = await _unitOfWork.Friendships.AreFriendsAsync(senderId, receiverId);
            var hasPending = await _unitOfWork.Friendships.HasPendingRequestAsync(senderId, receiverId);
            var isBlocked = await _unitOfWork.Friendships.IsUserBlockedAsync(senderId, receiverId);

            var validation = _friendshipDomainService.CanSendFriendRequest(senderId, receiverId, alreadyFriends, hasPending, isBlocked);
            if (!validation.IsSuccess)
                return Result.Failure(validation.Error);

            var friendship = new Friend
            {
                UserId = senderId,
                FriendUserId = receiverId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Friendships.Add(friendship);
            var saved = await _unitOfWork.CompleteAsync();
            return saved > 0 ? Result.Success() : Result.Failure("Failed to save friend request.");
        }

        public async Task<Result> RespondToRequestAsync(Guid userId, Guid requesterId, bool accept)
        {
            var friendship = await _unitOfWork.Friendships.GetByUsersAsync(requesterId, userId);
            if (friendship == null)
                return Result.Failure("Friend request not found.");

            var alreadyFriends = await _unitOfWork.Friendships.AreFriendsAsync(requesterId, userId);
            var validation = _friendshipDomainService.CanAcceptFriendRequest(friendship != null, alreadyFriends);
            if (!validation.IsSuccess)
                return Result.Failure(validation.Error);

            friendship.Status = accept ? FriendshipStatus.Accepted : FriendshipStatus.Rejected;
            _unitOfWork.Friendships.Update(friendship);

            var saved = await _unitOfWork.CompleteAsync();
            return saved > 0 ? Result.Success() : Result.Failure("Failed to update friend request.");
        }

        public async Task<Result<IEnumerable<FriendDto>>> GetFriendsListAsync(Guid userId)
        {
            var friends = await _unitOfWork.Friendships.GetListByUserAsync(userId);
            var acceptedFriends = friends.Where(f => f.Status == FriendshipStatus.Accepted);
            var dto = _mapper.Map<IEnumerable<FriendDto>>(acceptedFriends);

            return Result<IEnumerable<FriendDto>>.Success(dto);
        }

        public async Task<Result<IEnumerable<NotificationResponseDto>>> GetMyNotificationsAsync(Guid userId)
        {
            var notifications = await _unitOfWork.Notifications.GetUnreadNotificationsAsync(userId);
            var dto = _mapper.Map<IEnumerable<NotificationResponseDto>>(notifications);

            return Result<IEnumerable<NotificationResponseDto>>.Success(dto);
        }

        public async Task<Result> BlockUserAsync(Guid userId, Guid targetId)
        {
            var alreadyBlocked = await _unitOfWork.Friendships.IsUserBlockedAsync(userId, targetId);
            var validation = _friendshipDomainService.CanBlockUser(userId, targetId, alreadyBlocked);
            if (!validation.IsSuccess)
                return Result.Failure(validation.Error);

            var friendship = await _unitOfWork.Friendships.GetByUsersAsync(userId, targetId);
            if (friendship == null)
            {
                friendship = new Friend
                {
                    UserId = userId,
                    FriendUserId = targetId,
                    Status = FriendshipStatus.Blocked,
                    CreatedAt = DateTime.UtcNow
                };
                _unitOfWork.Friendships.Add(friendship);
            }
            else
            {
                friendship.Status = FriendshipStatus.Blocked;
                _unitOfWork.Friendships.Update(friendship);
            }

            var saved = await _unitOfWork.CompleteAsync();
            return saved > 0 ? Result.Success() : Result.Failure("Failed to block user.");
        }
    }

}
