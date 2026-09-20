using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
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
using Sohba.Converters;
using Sohba.Extensions;
using Sohba.Filters;
using Sohba.Handlers;
using Sohba.Hubs;
using Sohba.Infrastructure.Data;
using Sohba.Infrastructure.DependencyInjection;
using System;
using System.Text;
using System.Threading.Channels;
using System.Threading.RateLimiting;

namespace Sohba
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting Sohba application");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog((context, services, configuration) =>
                {
                    var serilogConfiguration = configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
                        .Enrich.WithProperty("Application", "Sohba")
                        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                        .WriteTo.Console()
                        .WriteTo.File(
                            path: "logs/sohba-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"
                        );

                    // Optional Elasticsearch log sink. Log storage only - application
                    // search stays SQL-based and no business data is sent to it.
                    // - Activated only when SOHBA_ES_URL is configured; the app
                    //   runs normally without Elasticsearch.
                    // - Events are written into a bounded in-memory channel and
                    //   shipped in the background. Under an Elasticsearch outage
                    //   the channel drops events instead of blocking requests.
                    // - The File sink above remains the durable fallback.
                    // - Credentials come from environment/secret configuration,
                    //   never from appsettings.json or source code.
                    var elasticsearchUrl = context.Configuration["SOHBA_ES_URL"];
                    Log.Information("Elasticsearch URL configured: {ElasticsearchUrl}", elasticsearchUrl ?? "<NULL>");
                    if (string.IsNullOrWhiteSpace(elasticsearchUrl))
                    {
                        return;
                    }

                    serilogConfiguration.WriteTo.Elasticsearch(
                        nodes: new[] { new Uri(elasticsearchUrl) },
                        configureOptions: options =>
                        {
                            // logs-sohba-<environment> data stream, e.g. logs-sohba-development
                            options.DataStream = new DataStreamName(
                                "logs",
                                "sohba",
                                context.HostingEnvironment.EnvironmentName.ToLowerInvariant());

                            // Install the ECS component/index templates for the data
                            // stream on first use; failures are silent so a missing or
                            // misconfigured Elasticsearch never breaks the app.
                            options.BootstrapMethod = BootstrapMethod.Silent;

                            // Optional native ILM policy for retention (e.g.
                            // a 7-day dev / 30-day prod policy). The policy must
                            // exist in Elasticsearch; when unset the shipped
                            // default "logs" policy applies.
                            var ilmPolicy = context.Configuration["SOHBA_ES_ILM_POLICY"];
                            if (!string.IsNullOrWhiteSpace(ilmPolicy))
                            {
                                options.IlmPolicy = ilmPolicy;
                            }

                            // Batching/backpressure: bounded inbound buffer with
                            // DropWrite guarantees logging can never add request
                            // latency when Elasticsearch is down or slow.
                            options.ConfigureChannel = channelOptions =>
                            {
                                channelOptions.BufferOptions = new BufferOptions
                                {
                                    InboundBufferMaxSize = 10_000,
                                    OutboundBufferMaxSize = 500,
                                    OutboundBufferMaxLifetime = TimeSpan.FromSeconds(5),
                                    ExportMaxConcurrency = 2,
                                    BoundedChannelFullMode = BoundedChannelFullMode.DropWrite
                                };
                            };
                        },
                        configureTransport: transportConfiguration =>
                        {
                            // Basic auth from secret configuration only
                            // (environment variables / user secrets).
                            var esUser = context.Configuration["SOHBA_ES_USER"];
                            var esPassword = context.Configuration["SOHBA_ES_PASSWORD"];
                            if (!string.IsNullOrEmpty(esUser) && !string.IsNullOrEmpty(esPassword))
                            {
                                transportConfiguration.Authentication(new BasicAuthentication(esUser, esPassword));
                            }
                        });
                });

                // JWT settings are validated before anything depends on them.
                var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
                if (jwtSettings != null)
                {
                    jwtSettings.Validate();
                    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
                }

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



                builder.Services.AddRateLimiter(options =>
                {
                    // Auth endpoints (Login, Register, ForgotPassword) - Partitioned by IP address
                    options.AddPolicy("Auth", httpContext =>
                    {
                        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 15,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                    });

                    options.AddPolicy("TokenRefresh", httpContext =>
                    {
                        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                    });

                    // API endpoints (Posts, Comments, Reactions, etc.) - Partitioned by User ID or IP
                    options.AddPolicy("Api", httpContext =>
                    {
                        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                           ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                           ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                    });

                    // Story creation endpoint - Partitioned by User ID or IP
                    options.AddPolicy("StoryCreate", httpContext =>
                    {
                        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                           ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                           ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                    });

                    // Feed endpoints (Home, LoadMore) - Partitioned by IP address
                    options.AddPolicy("Feed", httpContext =>
                    {
                        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                    });

                    // Friend requests - Partitioned by IP address
                    options.AddPolicy("FriendRequest", httpContext =>
                    {
                        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 2
                        });
                    });

                    // Search endpoints (QuickSearch, Results) - Partitioned by User ID or IP
                    options.AddPolicy("Search", httpContext =>
                    {
                        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                           ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                           ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                    });

                    // Dashboard (Admin only) - Partitioned by User ID (fallback to IP)
                    options.AddPolicy("Dashboard", httpContext =>
                    {
                        var partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                           ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                           ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 180,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 5
                        });
                    });

                    // Default - Partitioned by IP address
                    options.AddPolicy("Default", httpContext =>
                    {
                        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
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



                builder.Services.AddInfrastructureService(builder.Configuration);
                builder.Services.AddApplicationServices();
                builder.Services.AddSingleton(TimeProvider.System);

                builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

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

                builder.Services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
                    options.MaximumReceiveMessageSize = 1024 * 1024;
                });
                builder.Services.AddScoped<INotificationEventHandler, NotificationEventHandler>();

                builder.Services.AddControllersWithViews(options =>
                {
                    options.Filters.Add<ValidationFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                });

                // Allow the antiforgery token to be supplied via the
                // `X-CSRF-TOKEN` header (read by SohbaApp.post) so JSON AJAX
                // endpoints without a form body can still satisfy
                // [ValidateAntiForgeryToken].
                builder.Services.AddAntiforgery(options =>
                {
                    options.HeaderName = "X-CSRF-TOKEN";
                });

                builder.Services.AddFluentValidationAutoValidation();
                builder.Services.AddValidatorsFromAssemblyContaining<Sohba.Validators.PostCreateViewModelValidator>();
                builder.Services.AddValidatorsFromAssemblyContaining<Sohba.Application.Validators.CommentRequestDtoValidator>();

                var app = builder.Build();

                await app.InitializeDatabaseAsync();

                // Correlation + request logging wrap the entire pipeline.
                // CorrelationId reaches every log event, including the global
                // exception handler below.
                app.UseMiddleware<Sohba.Middleware.RequestCorrelationMiddleware>();
                app.UseMiddleware<Sohba.Middleware.RequestLoggingMiddleware>();
                // Global Exception Handler must execute at the start of the pipeline
                app.UseExceptionHandler(appError =>
                {
                    appError.Run(async context =>
                    {
                        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                        var exception = exceptionFeature?.Error;

                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

                        logger.LogError(
                            exception,
                            "Unhandled exception processing {Path} for user {UserId}",
                            context.Request.Path,
                            userId);

                        if (HttpErrorResponseHelper.IsAjaxOrJsonRequest(context.Request))
                        {
                            await HttpErrorResponseHelper.WriteJsonErrorAsync(
                                context.Response,
                                500,
                                "An unexpected error occurred.");
                        }
                        else
                        {
                            context.Response.Redirect("/Home/Error?code=500");
                        }
                    });
                });

                if (!app.Environment.IsDevelopment())
                {
                    app.UseHsts();
                }
                app.UseHttpsRedirection();
                // Production security-header baseline (CSP + nosniff + frame + referrer).
                app.UseSecurityHeaders();
                app.UseStaticFiles();
                app.UseRouting();

                app.UseRateLimiter();

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
