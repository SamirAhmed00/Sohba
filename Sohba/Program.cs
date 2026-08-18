using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Sohba.Application.DependencyInjection;
using Sohba.Application.Interfaces;
using Sohba.Application.Settings;
using Sohba.Extensions;
using Sohba.Handlers;
using Sohba.Hubs;
using Sohba.Infrastructure.DependencyInjection;
using System;
using System.Text;
using System.Threading.RateLimiting;

using Sohba.Converters;
using Sohba.Extensions;
using Sohba.Filters;

namespace Sohba
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // ============================================================
            // 0. SERILOG BOOTSTRAP
            // ============================================================
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/sohba-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"
                )
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting Sohba application");

                var builder = WebApplication.CreateBuilder(args);

                // ============================================================
                // أضف السطر ده عشان يحل محل الـ logging الافتراضي بـ Serilog
                // ============================================================
                builder.Host.UseSerilog((context, services, configuration) =>
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .WriteTo.File(
                            path: "logs/sohba-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"
                        ));

                // ============================================================
                // 1. REGISTER JWT SETTINGS 
                // ============================================================
                var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
                if (jwtSettings != null)
                {
                    jwtSettings.Validate();
                    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
                }

                // ============================================================
                // 2. ADD JWT AUTHENTICATION 
                // ============================================================
                var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing"));

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

                

                // ============================================================
                //  RATE LIMITING
                // ============================================================
                builder.Services.AddRateLimiter(options =>
                {
                    // Auth endpoints (Login, Register, ForgotPassword)
                    options.AddFixedWindowLimiter("Auth", opt =>
                    {
                        opt.PermitLimit = 5;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = 0;
                    });

                    // API endpoints (Posts, Comments, Reactions, etc.)
                    options.AddFixedWindowLimiter("Api", opt =>
                    {
                        opt.PermitLimit = 60;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = 0;
                    });

                    // Feed endpoints (Home, LoadMore)
                    options.AddFixedWindowLimiter("Feed", opt =>
                    {
                        opt.PermitLimit = 30;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = 0;
                    });

                    // Friend requests
                    options.AddFixedWindowLimiter("FriendRequest", opt =>
                    {
                        opt.PermitLimit = 30;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = 2;
                    });

                    // Dashboard (Admin only)
                    options.AddFixedWindowLimiter("Dashboard", opt =>
                    {
                        opt.PermitLimit = 20;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = 0;
                    });

                    // Default
                    options.AddFixedWindowLimiter("Default", opt =>
                    {
                        opt.PermitLimit = 100;
                        opt.Window = TimeSpan.FromMinutes(1);
                        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        opt.QueueLimit = 0;
                    });

                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                    options.OnRejected = async (context, token) =>
                    {
                        var httpContext = context.HttpContext;

                        if (HttpErrorResponseHelper.IsAjaxOrJsonRequest(httpContext.Request))
                        {
                            await HttpErrorResponseHelper.WriteJsonErrorAsync(
                                httpContext.Response,
                                StatusCodes.Status429TooManyRequests,
                                HttpErrorResponseHelper.GetFriendlyMessage(StatusCodes.Status429TooManyRequests));
                        }
                        else
                        {
                            httpContext.Response.Redirect("/Home/Error?code=429");
                        }
                    };
                });



                // ============================================================
                // 3. INFRASTRUCTURE & APPLICATION SERVICES 
                // ============================================================
                builder.Services.AddInfrastructureService(builder.Configuration);
                builder.Services.AddApplicationServices();
                builder.Services.AddHealthChecks();

                // COOKIE AUTH 
                builder.Services.AddAuthorization();

                builder.Services.ConfigureApplicationCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                    options.Cookie.IsEssential = true;
                    options.SlidingExpiration = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.MaxAge = null;
                    options.Cookie.Name = ".SohbaAuth";
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                });

                // ============================================================
                // 4. SIGNALR 
                // ============================================================
                builder.Services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                    options.MaximumReceiveMessageSize = 1024 * 1024;
                });
                builder.Services.AddScoped<INotificationEventHandler, NotificationEventHandler>();

                // ============================================================
                // 5. MVC & VALIDATION 
                // ============================================================
                builder.Services.AddControllersWithViews(options =>
                {
                    options.Filters.Add<ValidationFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                });

                builder.Services.AddFluentValidationAutoValidation();
                builder.Services.AddValidatorsFromAssemblyContaining<Sohba.Validators.PostCreateViewModelValidator>();
                builder.Services.AddValidatorsFromAssemblyContaining<Sohba.Application.Validators.CommentRequestDtoValidator>();

                // ============================================================
                // 6. BUILD APP
                // ============================================================
                var app = builder.Build();

                // ============================================================
                // 7. DATABASE INITIALIZATION 
                // ============================================================
                await app.InitializeDatabaseAsync();

                // ============================================================
                // 8. MIDDLEWARE PIPELINE 
                // ============================================================
                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseHsts();
                }
                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();

                app.UseStatusCodePages(async statusCodeContext =>
                {
                    var httpContext = statusCodeContext.HttpContext;
                    var response = httpContext.Response;

                    if (HttpErrorResponseHelper.IsAjaxOrJsonRequest(httpContext.Request))
                    {
                        await HttpErrorResponseHelper.WriteJsonErrorAsync(
                            response,
                            response.StatusCode,
                            HttpErrorResponseHelper.GetFriendlyMessage(response.StatusCode));
                    }
                    else
                    {
                        response.Redirect($"/Home/Error?code={response.StatusCode}");
                    }
                });

                app.UseAuthentication();
                app.UseAuthorization();

                app.UseRateLimiter();
                // ============================================================
                //  Global Exception Handler 
                // ============================================================
                app.UseExceptionHandler(appError =>
                {
                    appError.Run(async context =>
                    {
                        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                        var exception = exceptionFeature?.Error;

                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(exception, "Unhandled exception processing {Path}", context.Request.Path);

                        if (HttpErrorResponseHelper.IsAjaxOrJsonRequest(context.Request))
                        {
                            await HttpErrorResponseHelper.WriteJsonErrorAsync(
                                context.Response,
                                500,
                                "An unexpected error occurred.");
                            // correlationId still available to the client via the response body key "error";
                            // to keep the field name unchanged for existing consumers, WriteJsonErrorAsync's
                            // shape is {success:false, error:"..."} — add correlationId explicitly if any
                            // caller depends on it:
                        }
                        else
                        {
                            context.Response.Redirect("/Home/Error?code=500");
                        }
                    });
                });

                app.MapHub<NotificationHub>("/notificationHub");
                app.MapHealthChecks("/healthz");
                app.MapStaticAssets();
                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Landing}/{action=Index}/{id?}")
                    .WithStaticAssets();

                app.Run();

                Log.Information("Sohba application stopped gracefully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Sohba application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        
    }
}
