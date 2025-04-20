using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;
using System;

namespace Microsoft.AspNetCore.Builder;

public static class OpenApiRouteHandlerBuilderExtensions
{

#if NET8_0
    public static TBuilder ProducesProblem<TBuilder>(this TBuilder builder, int statusCode, string? contentType = null)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrEmpty(contentType))
        {
            contentType = ContentTypes.ProblemDetailsContentType;
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
            contentType = ContentTypes.ProblemDetailsContentType;
        }

        return builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, typeof(HttpValidationProblemDetails), [contentType]));
    }
#endif

    public static TBuilder Produces<TBuilder>(this TBuilder builder, 
        int statusCode,
        Type? responseType = null,
        string? contentType = null,
        params string[] additionalContentTypes)
        where TBuilder : IEndpointConventionBuilder
    {
        if (responseType is Type && string.IsNullOrEmpty(contentType))
        {
            contentType = ContentTypes.ApplicationJson;
        }

        if (contentType is null)
        {
            return builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, responseType ?? typeof(void)));
        }

        var contentTypes = new string[additionalContentTypes.Length + 1];
        contentTypes[0] = contentType;
        additionalContentTypes.CopyTo(contentTypes, 1);

        return builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, responseType ?? typeof(void), contentTypes));
    }
}
