using Microsoft.Extensions.DependencyInjection;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Domain_Rules.Logic;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Sohba.Application.DependencyInjection
{
    public static class ApplicationServiceContainer
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register AutoMapper and scan the current assembly for MappingProfile classes
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            });

            // Application Services Registration
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<ISocialService, SocialService>();
            services.AddScoped<IStoryService, StoryService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFriendshipService, FriendshipService>();
            services.AddScoped<IPageService, PageService>();
            services.AddScoped<IInteractionService, IInteractionService>();
            services.AddScoped<IReportingService, ReportingService>();

            // Domain Services Registration
            services.AddScoped<IFriendshipDomainService, FriendshipDomainService>();
            services.AddScoped<IGroupDomainService, GroupDomainService>();
            services.AddScoped<IInteractionDomainService, InteractionDomainService>();
            services.AddScoped<IMediaDomainService, MediaDomainService>();
            services.AddScoped<INotificationDomainService, NotificationDomainService>();
            services.AddScoped<IPostDomainService, PostDomainService>();
            services.AddScoped<IProfileDomainService, ProfileDomainService>();
            services.AddScoped<IReportingDomainService, ReportingDomainService>();
            services.AddScoped<IStoryDomainService, StoryDomainService>();


            return services;
        }
    }
}