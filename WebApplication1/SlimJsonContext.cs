using System.Text.Json.Serialization;
using WebApplication1.Endpoints.Products;

[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ValidationProblemDetails))]
[JsonSerializable(typeof(WebApplication1.Endpoints.WeatherForecast))]
[JsonSerializable(typeof(List<WebApplication1.Endpoints.WeatherForecast>))]
[JsonSerializable(typeof(WebApplication1.Endpoints.GetWeatherForecastRequest))]
[JsonSerializable(typeof(WebApplication1.Endpoints.UpdateWeatherForecastRequest))]
[JsonSerializable(typeof(WebApplication1.Endpoints.UpdateWeatherForecastsRequest))]
[JsonSerializable(typeof(Product[]))]
[JsonSerializable(typeof(WebApplication1.Endpoints.Products.PostSlug.SlugData))]
[JsonSerializable(typeof(WebApplication1.Endpoints.Products.Upload.UploadData))]
[JsonSerializable(typeof(WebApplication1.Endpoints.Products.Upload.UploadDataReturn))]
[JsonSerializable(typeof(IFormFile))]
[JsonSerializable(typeof(WebApplication1.Endpoints.UserNameClaim?))]
public partial class SlimJsonContext : JsonSerializerContext
{
}
