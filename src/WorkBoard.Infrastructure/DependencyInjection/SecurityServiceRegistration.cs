using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkBoard.Application.Interfaces;
using WorkBoard.Infrastructure.Services;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Infrastructure.DependencyInjection;

/// <summary>Registers hashing, token issuance and the injected clock.</summary>
public static class SecurityServiceRegistration
{
    /// <summary>Adds the security primitives the application layer depends on.</summary>
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()

            // Fails the host at startup rather than at the first login. A signing key that is
            // missing in production should never reach a request.
            .ValidateOnStart();

        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
