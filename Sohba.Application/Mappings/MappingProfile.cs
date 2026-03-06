using AutoMapper;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.SearchAggregate;
using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Entities.StoryAggregate;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sohba.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- User Mapping ---
            // Map Request DTO to Entity (For registration)
            CreateMap<UserRequestDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password)); // Basic mapping for now

            CreateMap<User, UserResponseDto>();

            // --- Post Mapping ---
            CreateMap<PostCreateDto, Post>();
            CreateMap<Post, PostResponseDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.SourceType, opt => opt.MapFrom(src => src.SourceType.ToString()))
                .ForMember(dest => dest.SourceName, opt => opt.MapFrom(src =>
                    src.SourceType == PostSourceType.Group && src.Group != null ? src.Group.Name :
                    src.SourceType == PostSourceType.Page && src.Page != null ? src.Page.Name :
                    null));

            // --- Comment Mapping ---
            CreateMap<CommentRequestDto, Comment>();
            CreateMap<Comment, CommentResponseDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name));

            // --- Group Mapping ---
            CreateMap<GroupCreateDto, Group>();
            CreateMap<GroupUpdateDto, Group>();
            CreateMap<Group, GroupResponseDto>()
                .ForMember(dest => dest.AdminName, opt => opt.MapFrom(src => src.Admin.Name));

            CreateMap<GroupMember, GroupMemberDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString())); // Enum to String

            // --- Page Mapping ---
            CreateMap<PageCreateDto, Page>();
            CreateMap<Page, PageResponseDto>()
                .ForMember(dest => dest.AdminName, opt => opt.MapFrom(src => src.Admin.Name))
                .ForMember(dest => dest.AdminId, opt => opt.MapFrom(src => src.AdminId))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

            CreateMap<PageFollower, PageFollowerDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name));

            // --- Reaction Mapping (Handling Enums) ---
            CreateMap<ReactionRequestDto, Reaction>();
            CreateMap<Reaction, ReactionResponseDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString())); // Enum to String

            // --- Post Report Mapping ---
            CreateMap<PostReportRequestDto, PostReport>();
            CreateMap<PostReport, PostReportResponseDto>()
                .ForMember(dest => dest.ReporterName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason.ToString()));

            // --- Saved Post Mapping ---
            CreateMap<SavedPost, SavedPostDto>()
                .ForMember(dest => dest.PostTitle, opt => opt.MapFrom(src => src.Post.Title))
                .ForMember(dest => dest.Tag, opt => opt.MapFrom(src => src.Tag.ToString()));

            // --- Notification & Friends ---
            CreateMap<Notification, NotificationResponseDto>()
                .ForMember(dest => dest.NotificationType, opt => opt.MapFrom(src => src.Type.ToString())); //

            CreateMap<Friend, FriendDto>()
                .ForMember(dest => dest.FriendName, opt => opt.MapFrom(src => src.FriendUser.Name))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.ProfilePictureUrl, opt => opt.MapFrom(src => src.FriendUser.ProfilePictureUrl));

            // --- Story Mapping ---
            CreateMap<Story, StoryResponseDto>()
             .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name))
             .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.User.Id))
             .ForMember(dest => dest.UserProfilePicture, opt => opt.MapFrom(src => src.User.ProfilePictureUrl));

            // --- Hashtag Mapping ---
            CreateMap<Hashtag, HashtagDto>();

            // Search mappings
            CreateMap<Post, PostSearchResultDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.Name));
            CreateMap<User, UserSearchResultDto>();
            CreateMap<Group, GroupSearchResultDto>()
                .ForMember(dest => dest.MembersCount, opt => opt.MapFrom(src => src.GroupMembers.Count));
            CreateMap<Page, PageSearchResultDto>();


            // RegisterDto -> AppUserDto
            CreateMap<RegisterDto, AppUserDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                // Ignore Id mapping if it's auto-generated, or map it properly
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()));

            // AppUserDto -> User (Domain)
            // Map the specific properties. Identity manager usually handles PasswordHash securely.
            CreateMap<AppUserDto, User>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                // Ignore PasswordHash here, as UserManager.CreateAsync handles hashing
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            // User -> AuthResponseDto
            CreateMap<User, AuthResponseDto>();
        }
    }
}
