using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkBoard.Infrastructure.DependencyInjection;

/// <summary>
/// The infrastructure layer's single entry point into the container.
/// </summary>
/// <remarks>
/// Deliberately two lines long. Registration is grouped by concern — persistence in
/// <see cref="PersistenceServiceRegistration"/>, security primitives in
/// <see cref="SecurityServiceRegistration"/> — because each group carries a page of explanation
/// about why it is configured the way it is, and one method holding all of it was the kind of file
/// nobody reads to the end.
/// </remarks>
public static class DependencyInjection
{
    /// <summary>Registers persistence, repositories, security primitives and the domain-event pipeline.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddPersistence(configuration)
            .AddSecurity(configuration);
}
