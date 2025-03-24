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
    }
}
