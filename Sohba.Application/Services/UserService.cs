using AutoMapper;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Application.Interfaces;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IProfileDomainService _profileDomainService;

        public UserService(IUnitOfWork unitOfWork, IMapper mapper, IProfileDomainService profileDomainService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _profileDomainService = profileDomainService;
        }

        public async Task<UserResponseDto> GetProfileAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            return _mapper.Map<UserResponseDto>(user);
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UserRequestDto userDto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return false;

            // Check domain rules for profile update
            var validation = _profileDomainService.CanUpdateProfile(userId, user.Id);
            if (!validation.IsSuccess) throw new Exception(validation.Error);

            _mapper.Map(userDto, user);
            _unitOfWork.Users.Update(user);
            return await _unitOfWork.CompleteAsync() > 0;
        }

        public async Task<IEnumerable<UserResponseDto>> SearchUsersAsync(string query)
        {
            // Simple search by username via repository
            var user = await _unitOfWork.Users.GetByUsernameAsync(query);
            if (user == null) return new List<UserResponseDto>();

            return new List<UserResponseDto> { _mapper.Map<UserResponseDto>(user) };
        }
    }
}
