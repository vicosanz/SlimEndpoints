﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private static readonly string iSlimEndpointWithoutRequest = "SlimEndpointWithoutRequest<";
        private static readonly string iSlimEndpointWithoutResponse = "SlimEndpointWithoutResponse<";

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

            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndEnums
                = context.CompilationProvider.Combine(typeDeclarations.Collect());
            
            context.RegisterSourceOutput(compilationAndEnums,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> type, SourceProductionContext context)
        {
            if (type.IsDefaultOrEmpty) return;

            List<Metadata> slimEndpoints = GetSlimpEndpoints(compilation, type.Distinct(), context);
            var groups = slimEndpoints.GroupBy(x => x.Group, StringComparer.InvariantCultureIgnoreCase);

            if (groups.Any())
            {
                var generatorAddSlimEndpoints = new AddSlimEndpointsWriter(slimEndpoints);
                context.AddSource($"{compilation.Assembly.MetadataName}.AddSlimEndpoints.g.cs",
                    SourceText.From(generatorAddSlimEndpoints.GetCode(), Encoding.UTF8));

                foreach (var group in groups)
                {
                    var generatorGroup = new UseSlimEndpointsWriter(group.Key, [.. group]);
                    context.AddSource($"{compilation.Assembly.MetadataName}.UseSlimEndpoints.{group.Key}.g.cs",
                        SourceText.From(generatorGroup.GetCode(), Encoding.UTF8));

                    foreach (var slimEndpoint in group)
                    {
                        var generatorSlimEndpoint = new SlimEndpointsWriter(slimEndpoint);
                        context.AddSource(slimEndpoint.GetFileNameImplementationGenerated(),
                            SourceText.From(generatorSlimEndpoint.GetCode(), Encoding.UTF8));
                    }
                }

                var generatorValidator = new ValidatorWriter(slimEndpoints);
                context.AddSource($"{compilation.Assembly.MetadataName}.Validators.g.cs",
                    SourceText.From(generatorValidator.GetCode(), Encoding.UTF8));
            }
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
                string requestType = "SlimEndpoints.AOT.Unit";
                string responseType = "SlimEndpoints.AOT.Unit";
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
                            requestType = requestTypeSyntax.ToFullString();
                            if (argumentsType.TypeArgumentList.Arguments.Count > 1)
                            {
                                responseType = argumentsType.TypeArgumentList.Arguments[1].ToFullString();
                            }
                        }
                        else if (baseType.ToFullString().Contains(iSlimEndpointWithoutRequest))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            responseType = argumentsType.TypeArgumentList.Arguments[0].ToFullString();
                        }
                        else if (baseType.ToFullString().Contains(iSlimEndpointWithoutResponse))
                        {
                            var argumentsType = (GenericNameSyntax)baseType.Type;
                            requestTypeSyntax = argumentsType.TypeArgumentList.Arguments[0];
                            requestType = requestTypeSyntax.ToFullString();
                        }
                    }
                }
                List<TypeProperty>? requestTypeSymbolProperties = [];
                string? auxiliarBodyRequestClassName = null;
                bool createAuxiliarBodyRequestClass = false;
                bool useAuxiliarBodyRequestClass = false;

                if (requestType != "SlimEndpoints.AOT.Unit")
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
