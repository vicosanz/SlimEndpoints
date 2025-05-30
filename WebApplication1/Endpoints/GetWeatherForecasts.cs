﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;

namespace WebApplication1.Endpoints;

[SlimEndpoint("", [SlimEndpoints.AOT.HttpMethods.Get], "weatherforecast")]
public class GetWeatherForecasts :
    SlimEndpointWithoutRequestProduce<List<WeatherForecast>>
{
    private static readonly string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public override void Configure(RouteHandlerBuilder builder)
    {
        builder
            .AllowAnonymous();
        builder.Produces<List<WeatherForecast>>(StatusCodes.Status200OK);
    }

    public override async Task<IResult> 
        HandleAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken);
        return TypedResults.Ok<List<WeatherForecast>>([..Enumerable.Range(1, 10).Select(index =>
                            new WeatherForecast
                            (
                                //DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                                Random.Shared.Next(-20, 55),
                                summaries[Random.Shared.Next(summaries.Length)]
                            ))]);
    }
}
