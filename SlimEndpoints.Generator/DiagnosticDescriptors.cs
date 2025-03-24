using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlimEndpoints.Generator
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor StructNotPartial =
            new(
                "SEI001",
                "Struct must be declared as 'readonly partial record struct'",
                "Struct {0} must be declared as 'readonly partial record struct'",
                DiagnosticCategories.SlimEndpoints,
                DiagnosticSeverity.Warning,
                true
            );
        public static readonly DiagnosticDescriptor ValidateMethodNotStatic =
            new(
                "SEI002",
                "Validate method must be declared as 'public static {0} Validate'",
                "Validate method must be declared as 'public static {0} Validate'",
                DiagnosticCategories.SlimEndpoints,
                DiagnosticSeverity.Warning,
                true
            );
        public static readonly DiagnosticDescriptor ConverterMethodNotStatic =
            new(
                "SEI003",
                "Converter method must be declared as 'public static AutoConverter<{0}, {1}> Converter'",
                "Converter method must be declared as 'public static AutoConverter<{0}, {1}> Converter'",
                DiagnosticCategories.SlimEndpoints,
                DiagnosticSeverity.Warning,
                true
            );
    }
}
