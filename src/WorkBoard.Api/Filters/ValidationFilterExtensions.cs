namespace WorkBoard.Api.Filters;

/// <summary>Attaches <see cref="ValidationFilter{T}"/> to an endpoint.</summary>
public static class ValidationFilterExtensions
{
    /// <summary>Validates <typeparamref name="T"/> before the endpoint handler runs.</summary>
    /// <typeparam name="T">The request body type to validate.</typeparam>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        where T : class =>
        builder
            .AddEndpointFilter<ValidationFilter<T>>()
            .ProducesValidationProblem();
}
