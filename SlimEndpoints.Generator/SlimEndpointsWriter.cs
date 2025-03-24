using Microsoft.CodeAnalysis;

namespace SlimEndpoints.Generator
{
    internal class SlimEndpointsWriter(Metadata metadata) : AbstractWriter
    {
        public string GetCode()
        {
            WriteFile();
            return GeneratedText();
        }

        private void WriteFile()
        {
            List<string> usings = ["using System;", $"using Microsoft.AspNetCore.Mvc;"];
            usings = [.. usings, .. metadata.Usings];
            foreach (var @using in usings.Distinct())
            {
                WriteLine(@using);
            }
            WriteLine();
            WriteLine("#nullable enable");
            WriteLine();

            if (!string.IsNullOrEmpty(metadata.Namespace))
            {
                WriteLine($"namespace {metadata.Namespace};");
            }
            WriteLine();
            WriteSlimEndpoints();
        }

        private void WriteSlimEndpoints()
        {
            WriteSlimEndpointImplementation();
        }

        private void WriteSlimEndpointImplementation()
        {
            WriteBrace($"{metadata.Modifiers} class {metadata.NameTyped}Implementation({metadata.NameTyped} endpoint)", () =>
            {
                WriteStatics();
            });
            WriteLine();
        }

        private void WriteStatics()
        {
            string propertiesWithTypeAndAnnotations = "";
            string propertiesWithType = "";
            string propertiesNames = "";
            string propertiesParse = "";
            string propertiesFromContext = "";
            bool isRequestFromBody = false;
            if (metadata.RequestTypeProperties != null)
            {
                if (metadata.Verb.Equals(HttpMehotds.Post, StringComparison.InvariantCultureIgnoreCase)
                    || metadata.Verb.Equals(HttpMehotds.Put, StringComparison.InvariantCultureIgnoreCase)
                    || metadata.Verb.Equals(HttpMehotds.Patch, StringComparison.InvariantCultureIgnoreCase))
                {
                    isRequestFromBody = true;
                    if (metadata.Route.Contains("{") ||
                        metadata.RequestTypeProperties.Any(x => 
                            x.Annotations.Contains(".FromRoute") || 
                            x.Annotations.Contains(".FromQuery") ||
                            x.Annotations.Contains(".FromHeader") ||
                            x.Annotations.Contains(".FromForm")
                        )
                    )
                    {
                        isRequestFromBody = false;
                    }
                }
                if (!isRequestFromBody)
                {
                    propertiesWithTypeAndAnnotations = string.Join(", ", metadata.RequestTypeProperties.Select(x => $"{x.Annotations}{x.Type} {x.Name}")) + ", ";
                    propertiesWithType = string.Join(", ", metadata.RequestTypeProperties.Select(x => $"{x.Type} {x.Name}")) + ", ";
                    propertiesNames = string.Join(", ", metadata.RequestTypeProperties?.Select(x => $"{x.Name}")) + ", ";
                    propertiesParse = string.Join(", ", metadata.RequestTypeProperties?.Select(x => $"{x.Name} = {x.Name}"));
                    int param = 0;
                    propertiesFromContext = string.Join(", ", metadata.RequestTypeProperties?.Select(x =>
                    {
                        param++;
                        return $"{x.Name} = context.GetArgument<{x.Type}>({param})";
                    }));
                }
                else
                {
                    propertiesWithTypeAndAnnotations = $"[FromBody] {metadata.RequestType} request, ";
                    propertiesWithType = $"{metadata.RequestType} request, ";
                    propertiesNames = "request, ";
                    propertiesParse = "";
                    propertiesFromContext = $"context.GetArgument<{metadata.RequestType}>(1)";
                }
            }

            WriteLine($"private readonly {metadata.NameTyped} endpoint = endpoint;");

            WriteLine();
            WriteBrace($"public void UseSlimEndpoint(IEndpointRouteBuilder app)", () =>
            {
                WriteLine($"var route = app.MapMethods(\"{metadata.Route}\", [\"{metadata.Verb}\"],");
                WriteLine($"    ([FromServices] {metadata.NameTyped}Implementation implementation, {propertiesWithTypeAndAnnotations}HttpContext httpContext, CancellationToken cancellationToken) =>");
                WriteLine($"        implementation.HandleAsync({propertiesNames}httpContext, cancellationToken));");
                WriteLine($"endpoint.Configure(route);");
            });
            string handle;
            if (metadata.ResponseType == "SlimEndpoints.Unit")
            {
                handle = $"public async Task HandleAsync({propertiesWithType}HttpContext httpContext, CancellationToken cancellationToken)";
            }
            else
            {
                handle = $"public async Task<{metadata.ResponseType}> HandleAsync({propertiesWithType}HttpContext httpContext, CancellationToken cancellationToken)";
            }
            WriteBrace(handle, () =>
            {
                if (metadata.RequestType == "SlimEndpoints.Unit")
                {
                    WriteLine($"return await endpoint.HandleAsync(httpContext, cancellationToken);");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(propertiesParse))
                    {
                        WriteLine($"var request = new {metadata.RequestType} {{{propertiesParse}}};");
                    }
                    WriteLine($"return await endpoint.HandleAsync(httpContext, request, cancellationToken);");
                }
            });
            WriteBrace($"public {metadata.RequestType} ParseRequestFromFilterContext(Microsoft.AspNetCore.Http.EndpointFilterInvocationContext context)", () =>
            {
                if (metadata.RequestType == "SlimEndpoints.Unit")
                {
                    WriteLine("var request = new SlimEndpoints.Unit();");
                }
                else if (isRequestFromBody)
                {
                    WriteLine($"var request = {propertiesFromContext};");
                }
                else
                {
                    WriteLine($"var request = new {metadata.RequestType}(){{ {propertiesFromContext} }};");
                }
                WriteLine("return request;");
            });

            WriteLine();
            WriteLine($"public IResult ValidateRequest({metadata.RequestType} request) => endpoint.Validate(request);");

            WriteLine();
            WriteLine("public IResult ValidateFromFilterContext(Microsoft.AspNetCore.Http.EndpointFilterInvocationContext context) => ValidateRequest(ParseRequestFromFilterContext(context));");
        }
    }
}