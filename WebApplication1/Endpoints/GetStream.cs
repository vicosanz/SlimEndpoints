using SlimEndpoints.AOT;
using System.Text;

namespace WebApplication1.Endpoints;

[SlimEndpoint("/stream/{id:int}", [SlimEndpoints.AOT.HttpMethods.Get], group: "weatherforecast")]
public class GetStream : SlimEndpointWithoutResponse<GetStreamRequest>
{
    public override async Task HandleAsync(HttpContext httpContext, GetStreamRequest request, CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = ContentTypes.TextPlain;

        await using var writer = new StreamWriter(httpContext.Response.Body);

        for (int i = 0; i < request.Id; i++)
        {
            await Task.Delay(100);
            var output = new StringBuilder();
            output.Append($"{i}");
            await writer.WriteLineAsync(output, cancellationToken: cancellationToken);
            await writer.FlushAsync(cancellationToken);
        }
    }
}


public record struct GetStreamRequest
{
    public int Id { get; set; }
}
