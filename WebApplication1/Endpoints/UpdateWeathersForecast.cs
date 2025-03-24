using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Endpoints;

public class UpdateWeatherForecastsRequest
{
    [Required]
    public string? Name { get; set; }
}

public class UpdateWeatherForecastsRequestValidator : AbstractValidator<UpdateWeatherForecastsRequest>
{
    public UpdateWeatherForecastsRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Must provide a name");
        RuleFor(x => x.Name).NotEqual("none").WithMessage("Name must not equal to none");
    }
}

[SlimEndpoint("/update", [HttpMehotds.Post], "weatherforecast")]
public partial class UpdateWeatherForecasts :
    SlimEndpoint<UpdateWeatherForecastsRequest,
        Results<
            Ok<WeatherForecast>,
            BadRequest<ProblemDetails>>>
{
    public override void Configure(RouteHandlerBuilder builder)
    {
        builder
            .AllowAnonymous();
    }

    public override IResult Validate(UpdateWeatherForecastsRequest request)
    {
        var validator = new UpdateWeatherForecastsRequestValidator();
        var result = validator.Validate(request);
        return result.OkOrProblem();
    }

    public override async Task<Results<Ok<WeatherForecast>, BadRequest<ProblemDetails>>>
        HandleAsync(HttpContext httpContext, UpdateWeatherForecastsRequest request, CancellationToken cancellationToken)
    {
        var result = TypedResults.Ok(new WeatherForecast(0, request.Name));
        return await Task.FromResult(result);
    }
}
