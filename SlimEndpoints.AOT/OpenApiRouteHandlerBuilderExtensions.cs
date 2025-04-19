#if NET8_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Builder;

public static class OpenApiRouteHandlerBuilderExtensions
{
    private static readonly string ProblemDetailsContentType = "application/problem+json";

    public static TBuilder ProducesProblem<TBuilder>(this TBuilder builder, int statusCode, string? contentType = null)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrEmpty(contentType))
        {
            contentType = ProblemDetailsContentType;
        }

        return builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, typeof(ProblemDetails), [contentType]));
    }

    public static TBuilder ProducesValidationProblem<TBuilder>(
        this TBuilder builder,
        int statusCode = StatusCodes.Status400BadRequest,
        string? contentType = null)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrEmpty(contentType))
        {
            contentType = ProblemDetailsContentType;
        }

        return builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, typeof(HttpValidationProblemDetails), [contentType]));
    }
}
#endif