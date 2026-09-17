using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WorkBoard.Infrastructure.Authentication;

namespace WorkBoard.Api.Extensions;

/// <summary>Configures JWT bearer authentication.</summary>
public static class AuthenticationRegistration
{
    /// <summary>
    /// Adds the bearer scheme and binds its validation parameters to <see cref="JwtOptions"/>.
    /// </summary>
    /// <remarks>
    /// Configured through the options pipeline rather than by reading <c>builder.Configuration</c>
    /// directly. Reading configuration eagerly at composition time looks equivalent and is not: it
    /// captures whatever sources exist at that instant, so anything added later — a test host
    /// injecting an in-memory source, a provider registered further down — is silently ignored, and
    /// the symptom is a zero-length signing key at the first request rather than an error at
    /// startup. Going through <c>IOptions</c> also means these values are the ones
    /// <c>ValidateOnStart</c> has already checked.
    /// </remarks>
    public static IServiceCollection AddWorkBoardAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

                    // The default allows five minutes of grace on expiry, which quietly extends
                    // every access token past the lifetime the design assumes. Zero makes expiry
                    // mean expiry.
                    ClockSkew = TimeSpan.Zero,

                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                };

                // Without this the handler rewrites "sub" to a long WS-Federation URI on the way in.
                bearer.MapInboundClaims = false;

                bearer.Events = new JwtBearerEvents { OnTokenValidated = NormalizeSubjectClaim };
            });

        return services;
    }

    /// <summary>
    /// Copies <c>sub</c> to <see cref="ClaimTypes.NameIdentifier"/> when only the former is present.
    /// </summary>
    /// <remarks>
    /// "sub" is the user id everywhere in the application. Mapping it once here means no other code
    /// has to know which of the two names it arrived under.
    /// </remarks>
    private static Task NormalizeSubjectClaim(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is ClaimsIdentity identity
            && identity.FindFirst(ClaimTypes.NameIdentifier) is null
            && identity.FindFirst("sub")?.Value is { } subject)
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
        }

        return Task.CompletedTask;
    }
}
