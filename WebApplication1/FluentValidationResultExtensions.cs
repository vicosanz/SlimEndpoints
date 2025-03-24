namespace WebApplication1
{
    public static class FluentValidationResultExtensions
    {
        public static IResult OkOrBadRequest(this FluentValidation.Results.ValidationResult result)
        {
            if (result.IsValid)
            {
                return Results.Ok();
            }
            return Results.Problem(title: "An error ocurred", statusCode: 400, extensions: result.Errors.Select(x => new KeyValuePair<string, object?>(x.PropertyName, x.ErrorMessage)));
        }

        public static IResult OkOrProblem(this FluentValidation.Results.ValidationResult result)
        {
            if (result.IsValid)
            {
                return Results.Ok();
            }
            return Results.Problem(title: "An error ocurred", extensions: result.Errors.Select(x => new KeyValuePair<string, object?>(x.PropertyName, x.ErrorMessage)));
        }
    }
}
