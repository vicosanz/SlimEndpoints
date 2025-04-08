using System.Text.Json.Serialization;
using WebApplication1.Endpoints.Products;
using WebApplication1.Endpoints.Products.PostSlug2;

namespace WebApplication1;

[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ValidationProblemDetails))]
[JsonSerializable(typeof(Endpoints.WeatherForecast))]
[JsonSerializable(typeof(List<Endpoints.WeatherForecast>))]
[JsonSerializable(typeof(Endpoints.GetWeatherForecastRequest))]
[JsonSerializable(typeof(Endpoints.UpdateWeatherForecastRequest))]
[JsonSerializable(typeof(Endpoints.UpdateWeatherForecastsRequest))]
[JsonSerializable(typeof(Product[]))]
[JsonSerializable(typeof(Endpoints.Products.PostSlug.SlugData))]
[JsonSerializable(typeof(Endpoints.Products.PostSlug2.Body))]
//[JsonSerializable(typeof(Endpoints.Products.PostSlug2.PostSlug2__BodyRequest))]
[JsonSerializable(typeof(PostSlug2Request))]
[JsonSerializable(typeof(Endpoints.Products.Upload.UploadData))]
[JsonSerializable(typeof(Endpoints.Products.Upload.UploadDataReturn))]
[JsonSerializable(typeof(IFormFile))]
[JsonSerializable(typeof(Endpoints.UserNameClaim?))]
public partial class SlimJsonContext : JsonSerializerContext;
