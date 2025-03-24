using Microsoft.AspNetCore.Http.HttpResults;

namespace WebApplication1
{
    public class ValidateRequestEndpointFilter1 : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var result = context.ValidateRequest();
            return result is Ok ? await next(context) : result;
        }
    }
}
