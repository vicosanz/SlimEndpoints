using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SlimEndpoints.AOT.Generator
{
    [Generator]
    public class SlimEndpointsGenerator : IIncrementalGenerator
    {
        private static readonly string slimEndpointAttribute = "SlimEndpoints.AOT.SlimEndpointAttribute";
        private static readonly string iSlimEndpointType = "SlimEndpoint<";
        private static readonly string iSlimEndpointProduceType = "SlimEndpointProduce<";

        private static readonly string iSlimEndpointWithoutRequestType = "SlimEndpointWithoutRequest<";
        private static readonly string iSlimEndpointWithoutRequestProduceType = "SlimEndpointWithoutRequestProduce<";

        private static readonly string iSlimEndpointWithoutResponse = "SlimEndpointWithoutResponse<";
        private static readonly string slimEndpointPipelineAttribute = "SlimEndpoints.AOT.SlimEndpointPipelineAttribute";
        //private static readonly string iSlimEndpointPipelineType = "SlimEndpointPipeline<";
        private static readonly string unit = "SlimEndpoints.AOT.Unit";
        private static readonly string iResult = "Microsoft.AspNetCore.Http.IResult";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached) Debugger.Launch();
//#endif
            IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s.IsSyntaxTargetForGeneration(),
                    transform: static (ctx, _) => ctx.GetSemanticTargetForGeneration(slimEndpointAttribute))
                .Where(static m => m is not null)!;

            IncrementalValuesProvider<TypeDeclarationSyntax> typePipelineDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s.IsSyntaxTargetForGeneration(),
                    transform: static (ctx, _) => ctx.GetSemanticTargetForGeneration(slimEndpointPipelineAttribute))
                .Where(static m => m is not null)!;

            var typesFound = typeDeclarations.Collect().Combine(typePipelineDeclarations.Collect());
            var compilationAndEnums = context.CompilationProvider.Combine(typesFound);

            context.RegisterSourceOutput(compilationAndEnums,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, 
            (ImmutableArray<TypeDeclarationSyntax> type, ImmutableArray<TypeDeclarationSyntax> pipelines) item2, SourceProductionContext context)
        {
            if (item2.type.IsDefaultOrEmpty) return;

            List<Metadata> slimEndpoints = GetSlimpEndpoints(compilation, item2.type.Distinct(), context);
            List<Pipeline> slimPipelines = GetSlimpPipelines(compilation, item2.pipelines.Distinct(), context).OrderBy(x=> x.Order).ToList();

            var groups = slimEndpoints.GroupBy(x => x.Group, StringComparer.InvariantCultureIgnoreCase);

            if (groups.Any())
            {
                var generatorAddSlimEndpoints = new AddSlimEndpointsWriter(slimEndpoints, slimPipelines);
                context.AddSource($"{compilation.Assembly.MetadataName}.AddSlimEndpoints.g.cs",
                    SourceText.From(generatorAddSlimEndpoints.GetCode(), Encoding.UTF8));

                var generatorGroup = new UseSlimEndpointsWriter(groups);
                context.AddSource($"{compilation.Assembly.MetadataName}.UseSlimEndpoints.g.cs",
                    SourceText.From(generatorGroup.GetCode(), Encoding.UTF8));

                foreach (var group in groups)
                {
                    foreach (var slimEndpoint in group)
                    {
                        var generatorSlimEndpoint = new SlimEndpointsWriter(slimEndpoint, slimPipelines);
                        context.AddSource(slimEndpoint.GetFileNameImplementationGenerated(),
                            SourceText.From(generatorSlimEndpoint.GetCode(), Encoding.UTF8));
                    }
                }

                var generatorValidator = new ValidatorWriter(slimEndpoints);
                context.AddSource($"{compilation.Assembly.MetadataName}.Validators.g.cs",
                    SourceText.From(generatorValidator.GetCode(), Encoding.UTF8));
            }
        }

        private static List<Pipeline> GetSlimpPipelines(Compilation compilation, IEnumerable<TypeDeclarationSyntax> types, SourceProductionContext context)
        {
            var slimPipelines = new List<Pipeline>();

            foreach (var type in types)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                SemanticModel semanticModel = compilation.GetSemanticModel(type.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(type) is not INamedTypeSymbol typeSymbol)
                {
                    // report diagnostic, something went wrong
                    continue;
                }

                int order = 0;
                foreach (var attribute in typeSymbol.GetAttributes())
                {
                    if (attribute.AttributeClass!.ToDisplayString().Equals(slimEndpointPipelineAttribute, StringComparison.OrdinalIgnoreCase))
                    {
                        if (attribute.ConstructorArguments.Any())
                        {
                            order = (int)attribute.ConstructorArguments[0].Value!;
                        }
                    }
                }

                if (typeSymbol.TypeArguments.Count() != 3)
                {
                    continue;
                }
                var argumentSlimEndpoint = typeSymbol.TypeArguments[0];
                var argumentRequest = typeSymbol.TypeArguments[1];
                var argumentResponse = typeSymbol.TypeArguments[2];

                var constructorParameters = typeSymbol.Constructors.FirstOrDefault()?.Parameters;

                slimPipelines.Add(new Pipeline()
                {
                    Namespace = type.GetNamespace(),
                    Usings = type.GetUsings(),
                    Name = typeSymbol.Name,
                    NameTyped = typeSymbol.GetNameTyped(),
                    Type = typeSymbol.ToString(),
                    Modifiers = type.GetModifiers(),
                    TypeSymbol = typeSymbol,
                    Order = order,
                    ConstructorParameters = constructorParameters,
                    ArgumentSlimEndpoint = argumentSlimEndpoint.ToString(),
                    ArgumentRequest = argumentRequest.ToString(),
                    ArgumentResponse = argumentResponse.ToString(),
                });
            }
            return slimPipelines;
        }

        protected static List<Metadata> GetSlimpEndpoints(Compilation compilation,
            IEnumerable<TypeDeclarationSyntax> types, SourceProductionContext context)
        {
            var slimEndpoints = new List<Metadata>();
            foreach (var type in types)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                SemanticModel semanticModel = compilation.GetSemanticModel(type.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(type) is not INamedTypeSymbol typeSymbol)
                {
                    // report diagnostic, something went wrong
                    continue;
                }
                string modifiers = type.GetModifiers();

                TypeSyntax? requestTypeSyntax = null;
                string requestType = unit;
                string responseType = unit;
                string? produceType = null;
                string? requestTypeKind = null;
                bool isRequestTypePositionRecord = false;

                if (type.BaseList != null)
                {
                    foreach (var baseType in type.BaseList!.Types)
                    {
                        if (baseType.ToFullString().Contains(iSlimEndpointType))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            requestTypeSyntax = argumentsType.TypeArgumentList.Arguments[0];
                            requestType = requestTypeSyntax.GetFullQualifiedName(semanticModel);
                            if (argumentsType.TypeArgumentList.Arguments.Count() > 1)
                            {
                                responseType = argumentsType.TypeArgumentList.Arguments[1].GetFullQualifiedName(semanticModel);
                            }
                            else
                            {
                                responseType = iResult;
                            }
                        }
                        else if (baseType.ToFullString().Contains(iSlimEndpointProduceType))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            requestTypeSyntax = argumentsType.TypeArgumentList.Arguments[0];
                            requestType = requestTypeSyntax.GetFullQualifiedName(semanticModel);
                            produceType = argumentsType.TypeArgumentList.Arguments[1].GetFullQualifiedName(semanticModel);
                            responseType = iResult;
                        }
                        else if (baseType.ToFullString().Contains(iSlimEndpointWithoutRequestType))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            responseType = argumentsType.TypeArgumentList.Arguments[0].GetFullQualifiedName(semanticModel);
                        }
                        else if (baseType.ToFullString().Contains(iSlimEndpointWithoutRequestProduceType))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            produceType = argumentsType.TypeArgumentList.Arguments[0].GetFullQualifiedName(semanticModel);
                            responseType = iResult;
                        }
                        else if (baseType.ToFullString().Contains(iSlimEndpointWithoutResponse))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            requestTypeSyntax = argumentsType.TypeArgumentList.Arguments[0];
                            requestType = requestTypeSyntax.GetFullQualifiedName(semanticModel);
                        }
                    }
                }
                List<TypeProperty>? requestTypeSymbolProperties = [];
                string? auxiliarBodyRequestClassName = null;
                bool createAuxiliarBodyRequestClass = false;
                bool useAuxiliarBodyRequestClass = false;

                if (requestType != unit)
                {
                    var requestTypeSymbol = semanticModel.GetTypeInfo(requestTypeSyntax!).Type;
                    if (requestTypeSymbol is null)
                    {
                        // report diagnostic, something went wrong
                        continue;
                    }
                    if (requestTypeSymbol.DeclaringSyntaxReferences.Length == 0)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(DiagnosticDescriptors.RequestTypeIsPrimitive, null, requestTypeSymbol.ToString(), typeSymbol.Name)
                        );
                        continue;
                    }
                    requestTypeKind = "class";
                    if (requestTypeSymbol.IsRecord)
                    {
                        requestTypeKind = "record class";
                        if (requestTypeSymbol.TypeKind == TypeKind.Struct)
                        {
                            requestTypeKind = "record struct";
                        }
                        else
                        {
                            requestTypeKind = "struct";
                        }
                    }
                    isRequestTypePositionRecord = requestTypeSymbol.IsRecord && ((INamedTypeSymbol)requestTypeSymbol).Constructors.Any(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length > 0);

                    var currentSymbol = requestTypeSymbol;
                    while (currentSymbol != null && currentSymbol.SpecialType != SpecialType.System_Object)
                    {
                        var baseTypeProperties = currentSymbol.GetMembers().OfType<IPropertySymbol>()
                            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                            .Select(x => new TypeProperty(currentSymbol, x.Type, x.Name, x.GetAnnotations(currentSymbol), x.HasBindParses()));
                        requestTypeSymbolProperties.AddRange(baseTypeProperties);

                        currentSymbol = currentSymbol.BaseType;
                    }

                    var fromBodys = requestTypeSymbolProperties.Where(x => x.HasFromBodyAnnotations()).ToList();
                    var fromBodyClass = fromBodys.Select(x => x.TypeSymbol).Distinct(SymbolEqualityComparer.Default).ToList();

                    if (fromBodyClass.Count == 1)
                    {
                        if (!requestTypeSymbolProperties
                            .Where(x => x.TypeSymbol.Equals(fromBodyClass[0], SymbolEqualityComparer.Default))
                            .Any(x => x.Annotations.Count == 0 || x.HasNonFromBodyAnnotations() || x.HasBindParses))
                        {
                            auxiliarBodyRequestClassName = fromBodyClass[0]!.ToString();
                            createAuxiliarBodyRequestClass = false;
                            useAuxiliarBodyRequestClass = true;
                        }
                        else if (fromBodys.Count() == 1 && !fromBodys.First().Type.IsAUserClass())
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(DiagnosticDescriptors.FromBodyIsPrimitive, null, requestTypeSymbol.ToString(), typeSymbol.Name)
                            );
                            createAuxiliarBodyRequestClass = true;
                            useAuxiliarBodyRequestClass = true;
                        }
                    }

                    var errorProperties = requestTypeSymbolProperties.
                        Where(x => x.HasBindParses && (
                            x.HasFromAnnotations()
                        ));

                    foreach (var errorProperty in errorProperties)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(DiagnosticDescriptors.PropertyWithBindAsyncOrTryParseAndFromAttribute, null, errorProperty.Name, requestTypeSymbol.ToString(), typeSymbol.Name)
                        );
                    }
                }

                string route = "";
                string[] verbs = [HttpMehotds.Get];
                string group = "";

                foreach (var attribute in typeSymbol.GetAttributes())
                {
                    if (attribute.AttributeClass!.ToDisplayString().Equals(slimEndpointAttribute, StringComparison.OrdinalIgnoreCase))
                    {
                        if (attribute.ConstructorArguments.Any())
                        {
                            route = attribute.ConstructorArguments.First().Value!.ToString();
                            if (attribute.ConstructorArguments.Count() > 1)
                            {
                                verbs = [.. attribute.ConstructorArguments[1].Values.Select(x => x.Value!.ToString())];
                            }
                            if (attribute.ConstructorArguments.Count() > 2)
                            {
                                group = attribute.ConstructorArguments[2].Value!.ToString();
                            }
                        }
                    }
                }

                string propertiesWithTypeAndAnnotations = "";
                string propertiesWithType = "";
                string propertiesNames = "";
                string propertiesParse = "";
                string propertiesFromContext = "";
                string parseinnerBodyRequest = "";
                string recordParametersBodyRequest = "";
                bool isRequestFromBody = false;
                bool isRequestAsParameter = false;
                if (requestTypeSymbolProperties.Count > 0)
                {
                    if (requestTypeKind == "struct" || requestTypeKind == "record" || requestTypeKind == "record struct")
                    {
                        isRequestAsParameter = true;
                    }
                    else if (verbs.Any(x => x.Equals(HttpMehotds.Post, StringComparison.InvariantCultureIgnoreCase)
                        || x.Equals(HttpMehotds.Put, StringComparison.InvariantCultureIgnoreCase)
                        || x.Equals(HttpMehotds.Patch, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        isRequestFromBody = true;
                        if (route.Contains("{") ||
                            requestTypeSymbolProperties.Any(x =>
                                x.HasBindParses ||
                                x.HasFromAnnotations()
                            )
                        )
                        {
                            isRequestFromBody = false;
                        }
                    }

                    if (!isRequestFromBody && !isRequestAsParameter)
                    {
                        if (useAuxiliarBodyRequestClass)
                        {
                            if (createAuxiliarBodyRequestClass)
                            {
                                auxiliarBodyRequestClassName = $"{typeSymbol.Name}__BodyRequest";
                            }

                            recordParametersBodyRequest = string.Join(", ", requestTypeSymbolProperties
                                .Where(x => x.HasFromBodyAnnotations()).Select(x => $"{x.Type} {x.Name}"));

                            propertiesWithTypeAndAnnotations = string.Join(", ", requestTypeSymbolProperties
                                .Where(x => !x.HasFromBodyAnnotations()).Select(x => $"{string.Join(" ", x.Annotations.Select(x => $"[{x}]"))}{x.Type} {x.Name}")) 
                                + $", [{GeneratorHelpers.FromBodyAttribute}] {auxiliarBodyRequestClassName} __request, ";

                            propertiesWithType = string.Join(", ", requestTypeSymbolProperties
                                .Where(x => !x.HasFromBodyAnnotations()).Select(x => $"{x.Type} {x.Name}")) + $", {auxiliarBodyRequestClassName} __request, ";

                            propertiesNames = string.Join(", ", requestTypeSymbolProperties?
                                .Where(x => !x.HasFromBodyAnnotations()).Select(x => $"{x.Name}")) + $", __request, ";

                            if (isRequestTypePositionRecord)
                            {
                                propertiesParse = string.Join(", ", requestTypeSymbolProperties?.Select(x =>
                                {
                                    if (x.HasFromBodyAnnotations())
                                    {
                                        return $"__request.{x.Name}";
                                    }
                                    return $"{x.Name}";
                                }));
                            }
                            else
                            {
                                propertiesParse = string.Join(", ", requestTypeSymbolProperties?.Select(x =>
                                {
                                    if (x.HasFromBodyAnnotations())
                                    {
                                        return $"{x.Name} = __request.{x.Name}";
                                    }
                                    return $"{x.Name} = {x.Name}";
                                }));
                            }

                            int param = 0;
                            propertiesFromContext = string.Join(", ", requestTypeSymbolProperties?
                                .Where(x => !x.HasFromBodyAnnotations()).Select(x =>
                                {
                                    param++;
                                    return isRequestTypePositionRecord
                                        ? $"context.GetArgument<{x.Type}>({param})"
                                        : $"{x.Name} = context.GetArgument<{x.Type}>({param})";
                                })) + ", ";

                            param++;
                            parseinnerBodyRequest = $"var __requestBody = context.GetArgument<{auxiliarBodyRequestClassName}>({param});";
                            propertiesFromContext += string.Join(", ", requestTypeSymbolProperties?
                                .Where(x => x.HasFromBodyAnnotations()).Select(x =>
                                {
                                    return isRequestTypePositionRecord
                                        ? $"__requestBody.{x.Name}"
                                        : $"{x.Name} = __requestBody.{x.Name}";
                                }));
                        }
                        else
                        {
                            propertiesWithTypeAndAnnotations = string.Join(", ", requestTypeSymbolProperties.Select(x => $"{string.Join(" ", x.Annotations.Select(x=> $"[{x}]"))}{x.Type} {x.Name}")) + ", ";
                            propertiesWithType = string.Join(", ", requestTypeSymbolProperties.Select(x => $"{x.Type} {x.Name}")) + ", ";
                            propertiesNames = string.Join(", ", requestTypeSymbolProperties?.Select(x => $"{x.Name}")) + ", ";
                            if (isRequestTypePositionRecord)
                            {
                                propertiesParse = string.Join(", ", requestTypeSymbolProperties?.Select(x => $"{x.Name}"));
                            }
                            else
                            {
                                propertiesParse = string.Join(", ", requestTypeSymbolProperties?.Select(x => $"{x.Name} = {x.Name}"));
                            }
                            int param = 0;
                            propertiesFromContext = string.Join(", ", requestTypeSymbolProperties?.Select(x =>
                            {
                                param++;
                                return isRequestTypePositionRecord
                                    ? $"context.GetArgument<{x.Type}>({param})"
                                    : $"{x.Name} = context.GetArgument<{x.Type}>({param})";
                            }));
                        }
                    }
                    else
                    {
                        string attribute = isRequestFromBody
                            ? $"[{GeneratorHelpers.FromBodyAttribute}]" :
                                isRequestAsParameter
                                    ? $"[{GeneratorHelpers.AsParametersAttribute}]"
                                    : "";
                        propertiesWithTypeAndAnnotations = $"{attribute} {requestType} request, ";
                        propertiesWithType = $"{requestType} request, ";
                        propertiesNames = "request, ";
                        propertiesParse = "";
                        propertiesFromContext = $"context.GetArgument<{requestType}>(1)";
                    }
                }

                slimEndpoints.Add(
                    new Metadata(type.GetNamespace(),
                                        type.GetUsings(),
                                        true,
                                        typeSymbol.Name,
                                        typeSymbol.GetNameTyped(),
                                        typeSymbol.ToString(),
                                        modifiers,
                                        requestType,
                                        produceType,
                                        responseType,
                                        requestTypeKind,
                                        isRequestTypePositionRecord,
                                        requestTypeSymbolProperties,
                                        route,
                                        verbs,
                                        group,

                                        propertiesWithTypeAndAnnotations,
                                        propertiesWithType,
                                        propertiesNames,
                                        propertiesParse,
                                        propertiesFromContext,
                                        isRequestFromBody,
                                        isRequestAsParameter,
                                        parseinnerBodyRequest,
                                        recordParametersBodyRequest,
                                        auxiliarBodyRequestClassName,
                                        createAuxiliarBodyRequestClass
                                        ));
            }
            return slimEndpoints;
        }

    }
}
