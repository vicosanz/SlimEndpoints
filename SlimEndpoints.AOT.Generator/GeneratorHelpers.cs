using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace SlimEndpoints.AOT.Generator
{
    public static class GeneratorHelpers
    {
        public static string AsBodyAttribute = "SlimEndpoints.AOT.AsBodyAttribute";

        public static string FromBodyAttribute = "Microsoft.AspNetCore.Mvc.FromBodyAttribute";
        public static string FromFormAttribute = "Microsoft.AspNetCore.Mvc.FromFormAttribute";
        public static string FromHeaderAttribute = "Microsoft.AspNetCore.Mvc.FromHeaderAttribute";
        public static string FromQueryAttribute = "Microsoft.AspNetCore.Mvc.FromQueryAttribute";
        public static string FromRouteAttribute = "Microsoft.AspNetCore.Mvc.FromRouteAttribute";
        public static string FromServicesAttribute = "Microsoft.AspNetCore.Mvc.FromServicesAttribute";

        public static string AsParametersAttribute = "Microsoft.AspNetCore.Http.AsParametersAttribute";

        public static bool IsTypePrimitiveOrId(string type) => type switch
        {
            "string" or "bool" or "byte" or "DateTime" or "DateTimeOffset" or
            "decimal" or "double" or "Guid" or "short" or "Ulid" or "int" or "sbyte" or "float" or
            "TimeSpan" or "ushort" or "uint" or "char" or "long" or "ulong" => true,
            _ => false
        };

        public static string GetNamespace(this BaseTypeDeclarationSyntax syntax)
        {
            var result = string.Empty;
            SyntaxNode? potentialNamespaceParent = syntax.Parent;

            while (potentialNamespaceParent != null &&
                    potentialNamespaceParent is not NamespaceDeclarationSyntax
                    && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                result = namespaceParent.Name.ToString();

                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    result = $"{namespaceParent.Name}.{result}";
                    namespaceParent = parent;
                }
            }

            return result;
        }

        public static string GetModifiers(this BaseTypeDeclarationSyntax syntax)
        {
            return syntax.Modifiers.ToString();
        }

        public static string GetNameTyped(this INamedTypeSymbol symbol)
        {
            if (symbol.TypeArguments.Any())
            {
                return $"{symbol.Name}<" + string.Join(", ", symbol.TypeArguments.ToList().ConvertAll(x => x.ToString())) + ">";
            }
            else
            {
                return symbol.Name;
            }
        }

        public static IReadOnlyList<string> GetUsings(this BaseTypeDeclarationSyntax syntax)
        {
            SyntaxNode? parent = syntax.Parent;
            while (parent != null)
            {
                if (parent is CompilationUnitSyntax compilationUnit)
                {
                    return compilationUnit.Usings.ToList().ConvertAll(x => Regex.Replace(x.ToFullString(), @"using\s+|;|\r|\n", ""));
                }
                parent = parent.Parent;
            }
            return [];
        }

        public static bool IsSyntaxTargetForGeneration(this SyntaxNode node)
            => node is TypeDeclarationSyntax m && m.AttributeLists.Count > 0;

        public static TypeDeclarationSyntax? GetSemanticTargetForGeneration(this GeneratorSyntaxContext context, string attribute)
        {
            var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

            foreach (AttributeListSyntax attributeListSyntax in typeDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == attribute)
                    {
                        return typeDeclarationSyntax;
                    }
                }
            }
            return null;
        }

        public static string GetFileNameImplementationGenerated(this Metadata metadata)
            => $"{metadata.FullName.Replace('<', '_').Replace('>', '_')}Implementation.g.cs";

        public static List<string> GetAnnotations(this IPropertySymbol x, ITypeSymbol typeSymbol)
        {
            var attributes = x.GetAttributes().Select(x => x.ToString()).ToList();
            if (typeSymbol.GetAttributes().Any(x => x.ToString() == AsBodyAttribute)
                && !attributes.Any(x => x.ToString().StartsWith(FromBodyAttribute)))
            {
                attributes.Add(FromBodyAttribute);
            }
            //if (attributes.Count == 0) return [];
            return attributes;
        }

        public static bool HasBindParses(this IPropertySymbol x)
        {
            ITypeSymbol type = x.Type;
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                type = ((INamedTypeSymbol)type).TypeArguments[0];
            }
            if (type.DeclaringSyntaxReferences.Length == 0) return false;
            return type.GetMembers().Where(x => x.IsStatic && (x.Name == "BindAsync" || x.Name == "TryParse")).Any();
        }

        public static bool HasFromAnnotations(this TypeProperty typeProperty) => typeProperty.Annotations.Any(x => x.StartsWith(FromBodyAttribute)
                                                                                              || x.StartsWith(FromFormAttribute)
                                                                                              || x.StartsWith(FromHeaderAttribute)
                                                                                              || x.StartsWith(FromQueryAttribute)
                                                                                              || x.StartsWith(FromRouteAttribute));

        public static bool HasFromBodyAnnotations(this TypeProperty typeProperty) => typeProperty.Annotations.Any(x => x.StartsWith(FromBodyAttribute));

        public static bool HasNonFromBodyAnnotations(this TypeProperty typeProperty) => typeProperty.Annotations.Any(x => x.StartsWith(FromFormAttribute)
                                                                                              || x.StartsWith(FromHeaderAttribute)
                                                                                              || x.StartsWith(FromQueryAttribute)
                                                                                              || x.StartsWith(FromRouteAttribute));
    
        public static bool IsAUserClass(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind == TypeKind.Class || typeSymbol.TypeKind == TypeKind.Struct || typeSymbol.IsRecord)
            {
                return typeSymbol.SpecialType == SpecialType.None && typeSymbol.ContainingNamespace.ToString() != "System";
            }
            
            return false;
        }

        public static string GetFullQualifiedName(this TypeSyntax typeSyntax, SemanticModel semanticModel)
        {
            return semanticModel.GetSymbolInfo(typeSyntax).Symbol switch
            {
                ITypeSymbol typeSymbol => typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                INamespaceSymbol namespaceSymbol => namespaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IMethodSymbol methodSymbol => methodSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IPropertySymbol propertySymbol => propertySymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IEventSymbol eventSymbol => eventSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                _ => string.Empty
            };
        }
    }
}
