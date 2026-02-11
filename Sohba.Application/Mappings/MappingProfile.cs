using AutoMapper;
using Sohba.Application.DTOs.GroupAndPageAggregate;
using Sohba.Application.DTOs.PostAggregate;
using Sohba.Application.DTOs.StoryAggregate;
using Sohba.Application.DTOs.UserAggregate;
using Sohba.Domain.Entities;
using Sohba.Domain.Entities.GroupAndPage;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Entities.UserAggregate;
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
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.Name));

            // --- Comment Mapping ---
            CreateMap<CommentRequestDto, Comment>();
            CreateMap<Comment, CommentResponseDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name));

            // --- Group Mapping ---
            CreateMap<GroupCreateDto, Group>();
            CreateMap<Group, GroupResponseDto>()
                .ForMember(dest => dest.AdminName, opt => opt.MapFrom(src => src.Admin.Name));

            CreateMap<GroupMember, GroupMemberDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString())); // Enum to String

            // --- Page Mapping ---
            CreateMap<PageCreateDto, Page>();
            CreateMap<Page, PageResponseDto>()
                .ForMember(dest => dest.AdminName, opt => opt.MapFrom(src => src.Admin.Name));

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
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name));

            // --- Hashtag Mapping ---
            CreateMap<Hashtag, HashtagDto>();
        }
    }
}
