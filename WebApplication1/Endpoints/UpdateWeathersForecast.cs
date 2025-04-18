using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SlimEndpoints.AOT;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Endpoints;

public class UpdateWeatherForecastsRequest
{
    [FromBody]
    public UpdateWeatherForecastsRequestBase Base { get; set; } = null!;
    public UserNameClaim? UserName { get; set; }
}

public class UpdateWeatherForecastsRequestBase
{
    [Required]
    public string? Name { get; set; }
}

public class UpdateWeatherForecastsRequestValidator : AbstractValidator<UpdateWeatherForecastsRequest>
{
    public UpdateWeatherForecastsRequestValidator()
    {
        RuleFor(x => x.Base.Name).NotEmpty().WithMessage("Must provide a name");
        RuleFor(x => x.Base.Name).NotEqual("none").WithMessage("Name must not equal to none");
    }
}

[SlimEndpoint("/update", [SlimEndpoints.AOT.HttpMethods.Post], "weatherforecast")]
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
        var result = TypedResults.Ok(new WeatherForecast(0, request.Base.Name));
        return await Task.FromResult(result);
    }
}
