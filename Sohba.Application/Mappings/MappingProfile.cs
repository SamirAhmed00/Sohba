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
using System.Text.Json;

namespace Sohba.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- User Mapping ---
            // Map Request DTO to Entity (For registration)
            CreateMap<UserRequestDto, User>();

            CreateMap<User, UserResponseDto>();

            // --- Post Mapping ---
            CreateMap<PostCreateDto, Post>().ForMember(dest => dest.ImageUrls, opt => opt.Ignore());
            CreateMap<PostUpdateDto, Post>();
            CreateMap<Post, PostResponseDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.IsPrivate, opt => opt.MapFrom(src => src.IsPrivate))
                .ForMember(dest => dest.Privacy, opt => opt.MapFrom(src => src.Privacy))
                .ForMember(dest => dest.SourceType, opt => opt.MapFrom(src => src.SourceType.ToString()))
                .ForMember(dest => dest.SourceName, opt => opt.MapFrom(src =>
                    src.SourceType == PostSourceType.Group && src.Group != null ? src.Group.Name :
                    src.SourceType == PostSourceType.Page && src.Page != null ? src.Page.Name :
                    null))
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src => DeserializePostImageUrls(src.ImageUrls)));

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

            // --- Saved Collection Mapping ---
            CreateMap<SavedCollection, SavedCollectionDto>()
                .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => src.SavedPosts != null ? src.SavedPosts.Count : 0));

            // --- Notification & Friends ---
            CreateMap<Notification, NotificationResponseDto>()
                .ForMember(dest => dest.NotificationType, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.Name : "System"))
                .ForMember(dest => dest.SenderProfilePicture, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.ProfilePictureUrl : null))
                .ForMember(dest => dest.TimeAgo, opt => opt.Ignore());

            // -- Unused Friend Mapping (Commented Out) ---
            //CreateMap<Friend, FriendDto>()
            //     .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId)) 
            //     .ForMember(dest => dest.FriendUserId, opt => opt.MapFrom(src => src.FriendUserId))
            //     .ForMember(dest => dest.FriendName, opt => opt.MapFrom(src => src.User.Name))  
            //     .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.FriendUser.Name))
            //     .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            //     .ForMember(dest => dest.ProfilePictureUrl, opt => opt.MapFrom(src => src.User.ProfilePictureUrl));


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


            //// RegisterDto -> AppUserDto
            //CreateMap<RegisterDto, AppUserDto>()
            //    .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            //    .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => false))
            //    .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            //    // Ignore Id mapping if it's auto-generated, or map it properly
            //    .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()));

            //// AppUserDto -> User (Domain)
            //// Map the specific properties. Identity manager usually handles PasswordHash securely.
            //CreateMap<AppUserDto, User>()
            //    .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            //    .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
            //    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            //    // Ignore PasswordHash here, as UserManager.CreateAsync handles hashing
            //    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            // User -> AuthResponseDto
            CreateMap<User, AuthResponseDto>();

            // Notification Mapping
            CreateMap<Notification, NotificationResponseDto>()
                .ForMember(dest => dest.NotificationType, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.Name : "System"))
                .ForMember(dest => dest.SenderProfilePicture, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.ProfilePictureUrl : null));


    
        }
        // --- Helper Class ---
        private static List<string> DeserializePostImageUrls(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
