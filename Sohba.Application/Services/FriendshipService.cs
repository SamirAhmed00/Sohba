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
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipDomainService _domainService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public FriendshipService(
            IFriendshipDomainService domainService,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _domainService = domainService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // Friend Requests
        public async Task<Result> SendFriendRequestAsync(Guid senderId, Guid receiverId)
        {
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
                return decision;

            var friendRequest = new Friend
            {
                UserId = senderId,
                FriendUserId = receiverId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Friendships.Add(friendRequest);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> AcceptFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            var hasPending = await _unitOfWork.Friendships.HasPendingRequestAsync(senderId, receiverId);
            var alreadyFriends = await _unitOfWork.Friendships.AreFriendsAsync(senderId, receiverId);

            var decision = _domainService.CanAcceptFriendRequest(hasPending, alreadyFriends);

            if (!decision.IsSuccess)
                return decision;

            var friendship = await _unitOfWork.Friendships.GetByUsersAsync(senderId, receiverId);

            if (friendship == null)
                return Result.Failure("Friend request not found.");

            friendship.Status = FriendshipStatus.Accepted;

            _unitOfWork.Friendships.Update(friendship);
            await _unitOfWork.CompleteAsync();

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
            var dto = _mapper.Map<IEnumerable<FriendDto>>(friends);
            return Result<IEnumerable<FriendDto>>.Success(dto);
        }


        public async Task<Result<IEnumerable<FriendDto>>> GetPendingRequestsAsync(Guid userId)
        {
            var requests = await _unitOfWork.Friendships.GetPendingRequestsAsync(userId);
            var dto = _mapper.Map<IEnumerable<FriendDto>>(requests);
            return Result<IEnumerable<FriendDto>>.Success(dto);
        }

        public async Task<Result<IEnumerable<FriendDto>>> GetSentRequestsAsync(Guid userId)
        {
            var requests = await _unitOfWork.Friendships.GetSentRequestsAsync(userId);
            var dto = _mapper.Map<IEnumerable<FriendDto>>(requests);
            return Result<IEnumerable<FriendDto>>.Success(dto);
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
                return validation;

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
            var dto = _mapper.Map<IEnumerable<FriendDto>>(blocked);
            return Result<IEnumerable<FriendDto>>.Success(dto);
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