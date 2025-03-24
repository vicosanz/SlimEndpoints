using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Endpoints;

public class UpdateWeatherForecastRequest
{
    [FromRoute]
    public int Id { get; set; }
    [FromBody]
    [Required]
    public UpdateWeatherForecastsRequest? Values { get; set; }
}

[SlimEndpoint("/update/{Id}", HttpMehotds.Post, "weatherforecast")]
public class UpdateWeatherForecast :
    SlimEndpoint<UpdateWeatherForecastRequest,
        Results<
            Ok<WeatherForecast>,
            BadRequest<ProblemDetails>,
            InternalServerError<ProblemDetails>>>
{
    public override void Configure(RouteHandlerBuilder builder)
    {
        builder
            .AllowAnonymous();
    }

    public override async Task<Results<Ok<WeatherForecast>, BadRequest<ProblemDetails>, InternalServerError<ProblemDetails>>>
        HandleAsync(HttpContext httpContext, UpdateWeatherForecastRequest request, CancellationToken cancellationToken)
    {
        var result = TypedResults.Ok(new WeatherForecast(0, request.Values!.Name));
        return await Task.FromResult(result);
    }
}
