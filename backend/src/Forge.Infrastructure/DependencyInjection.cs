using Forge.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<ISignInThrottle, SignInThrottle>();
        services.AddScoped<IPasswordResetEmailSender, Deferred.LoggingPasswordResetEmailSender>();
        services.AddScoped<IHealthKitIngest, Deferred.LoggingHealthKitIngest>();
        services.AddScoped<INotificationSender, Deferred.LoggingNotificationSender>();
        services.AddScoped<IPointsScorer, PointsScorer>();
        services.AddScoped<Forge.Application.Notifications.INotificationDispatcher, NotificationDispatcher>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}
