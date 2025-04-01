using ComplexType;

namespace WebApplication1.Endpoints
{
    [ComplexType]
    public readonly partial record struct UserNameClaim
    {
        public static ValueTask<UserNameClaim?> BindAsync(HttpContext httpContext)
        {
            var claimValue = httpContext.User?.Claims?.FirstOrDefault(c => c.Type == "UserId")?.Value;
            UserNameClaim? result = null;
            if (!string.IsNullOrWhiteSpace(claimValue))
            {
                result = claimValue;
            }
            return ValueTask.FromResult(result);
        }
    }
}
