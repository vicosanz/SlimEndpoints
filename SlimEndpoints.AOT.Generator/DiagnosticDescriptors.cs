using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimEndpoints.AOT.Generator
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor RequestTypeIsPrimitive =
            new(
                "SEI001",
                "{0} type cannot be used as {1} endpoint request, create a User class or record instead",
                "{0} type cannot be used as {1} endpoint request, create a User class or record instead",
                DiagnosticCategories.SlimEndpoints,
                DiagnosticSeverity.Error,
                true
            );

        public static readonly DiagnosticDescriptor PropertyWithBindAsyncOrTryParseAndFromAttribute =
            new(
                "SEI002",
                "{0} property of {1} type in {2} endpoint request with BindAsync or TryParse should not have [From...] attributes",
                "{0} property of {1} type in {2} endpoint request with BindAsync or TryParse should not have [From...] attributes",
                DiagnosticCategories.SlimEndpoints,
                DiagnosticSeverity.Warning,
                true
            );

        public static readonly DiagnosticDescriptor FromBodyIsPrimitive =
            new(
                "SEI003",
                "{0} type in {1} endpoint request has a primitive as FromBody, use a class or record parameter instead",
                "{0} type in {1} endpoint request has a primitive as FromBody, use a class or record parameter instead",
                DiagnosticCategories.SlimEndpoints,
                DiagnosticSeverity.Warning,
                true
            );
    }
}
