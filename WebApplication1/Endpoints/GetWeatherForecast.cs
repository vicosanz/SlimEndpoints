using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SlimEndpoints.AOT;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Endpoints;

public class GetWeatherForecastRequest
{
    [FromRoute(Name = "Id")]
    [Required]
    public int Id { get; set; }
    [FromQuery]
    public string? Name { get; set; }
    [FromHeader(Name = "Accept")]
    public string? accept { get; set; }
}

[SlimEndpoint("/{Id}", [SlimEndpoints.AOT.HttpMethods.Get], "weatherforecast")]
public class GetWeatherForecast(IMemoryCache memoryCache) :
    SlimEndpoint<GetWeatherForecastRequest,
        Results<
            Ok<List<WeatherForecast>>,
            BadRequest<ProblemDetails>,
            InternalServerError<ProblemDetails>>>
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private static readonly string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public override void Configure(RouteHandlerBuilder builder)
    {
        builder
            .WithName("xxx")
            .AllowAnonymous();
    }

    public override async Task<Results<Ok<List<WeatherForecast>>, BadRequest<ProblemDetails>, InternalServerError<ProblemDetails>>> 
        HandleAsync(HttpContext httpContext, GetWeatherForecastRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken);
        if (request.Id == 0) return TypedResults.BadRequest(TypedResults.Problem("id must be greater than 0", statusCode: 400).ProblemDetails);
        return _memoryCache.GetOrCreate($"weatherforecast{request.Id}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
            return TypedResults.Ok<List<WeatherForecast>>([..Enumerable.Range(1, 1).Select(index =>
                                new WeatherForecast
                                (
                                    //DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                                    Random.Shared.Next(-20, 55),
                                    summaries[Random.Shared.Next(summaries.Length)]
                                ))]);
        })!;
    }
}
