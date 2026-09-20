using Microsoft.Extensions.DependencyInjection;
using Sohba.Application.Interfaces;
using Sohba.Application.Services;
using Sohba.Domain.Domain_Rules.Interface;
using Sohba.Domain.Domain_Rules.Logic;
using System.Reflection;

namespace Sohba.Application.DependencyInjection
{
    public static class ApplicationServiceContainer
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMemoryCache();

            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(Assembly.GetExecutingAssembly());
            });

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IStoryService, StoryService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFriendshipService, FriendshipService>();
            services.AddScoped<IPageService, PageService>();
            services.AddScoped<IInteractionService, InteractionService>();
            services.AddScoped<IReportingService, ReportingService>();
            services.AddScoped<IHashtagService, HashtagService>();
            services.AddScoped<ISearchService, SearchService>();
            services.AddScoped<IUserSettingsService, UserSettingsService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();

            services.AddScoped<IFriendshipDomainService, FriendshipDomainService>();
            services.AddScoped<IGroupDomainService, GroupDomainService>();
            services.AddScoped<IInteractionDomainService, InteractionDomainService>();
            services.AddScoped<IMediaDomainService, MediaDomainService>();
            services.AddScoped<INotificationDomainService, NotificationDomainService>();
            services.AddScoped<IPostDomainService, PostDomainService>();
            services.AddScoped<IProfileDomainService, ProfileDomainService>();
            services.AddScoped<IReportingDomainService, ReportingDomainService>();
            services.AddScoped<IStoryDomainService, StoryDomainService>();
            services.AddScoped<IPageDomainService, PageDomainService>();

            return services;
        }
    }
}