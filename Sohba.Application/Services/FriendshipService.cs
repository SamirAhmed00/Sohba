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
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IFriendshipDomainService _domainService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public FriendshipService(
            IFriendshipRepository friendshipRepository,
            IFriendshipDomainService domainService,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _friendshipRepository = friendshipRepository;
            _domainService = domainService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }


        public async Task<Result> SendFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            var alreadyFriends =
                await _friendshipRepository.AreFriendsAsync(senderId, receiverId);

            var hasPending =
                await _friendshipRepository.HasPendingRequestAsync(senderId, receiverId);

            var isBlocked =
                await _friendshipRepository.IsUserBlockedAsync(senderId, receiverId);

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

            _friendshipRepository.Add(friendRequest);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }


        public async Task<Result> AcceptFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            var hasPending =
                await _friendshipRepository.HasPendingRequestAsync(senderId, receiverId);

            var alreadyFriends = await _friendshipRepository.AreFriendsAsync(senderId, receiverId);
            var decision = _domainService.CanAcceptFriendRequest(hasPending, alreadyFriends);

            if (!decision.IsSuccess) 
                return decision;

            var friendship =
                await _friendshipRepository.GetByUsersAsync(senderId, receiverId);

            if (friendship == null)
                return Result.Failure("Friend request not found.");

            friendship.Status = FriendshipStatus.Accepted;

            _friendshipRepository.Update(friendship);
            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }

        public async Task<Result> RejectFriendRequestAsync(Guid senderId, Guid receiverId)
        {
            var hasPending =
                await _friendshipRepository.HasPendingRequestAsync(senderId, receiverId);

            var decision = _domainService.CanDeclineFriendRequest(hasPending);
            if (!decision.IsSuccess)
                return decision;

            var friendship =
                await _friendshipRepository.GetByUsersAsync(senderId, receiverId);

            if (friendship != null)
            {
                _friendshipRepository.Delete(friendship);
                await _unitOfWork.CompleteAsync();
            }

            return Result.Success();
        }



        public async Task<Result> UnfriendAsync(Guid userId, Guid friendId)
        {
            var alreadyFriends =
                await _friendshipRepository.AreFriendsAsync(userId, friendId);

            var decision = _domainService.CanRemoveFriend(alreadyFriends);

            if (!decision.IsSuccess)
                return decision;

            var friendship =
                await _friendshipRepository.GetByUsersAsync(userId, friendId);

            var reverseFriendship =
                await _friendshipRepository.GetByUsersAsync(friendId, userId);

            if (friendship != null)
                _friendshipRepository.Delete(friendship);

            if (reverseFriendship != null)
                _friendshipRepository.Delete(reverseFriendship);

            await _unitOfWork.CompleteAsync();

            return Result.Success();
        }


        public async Task<Result<IEnumerable<FriendDto>>> GetFriendsListAsync(Guid userId)
        {
            var friends =
                await _friendshipRepository.GetListByUserAsync(userId);

            var dto =
                _mapper.Map<IEnumerable<FriendDto>>(friends);

            return Result<IEnumerable<FriendDto>>.Success(dto);
        }
    }

}
