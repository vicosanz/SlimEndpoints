using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace SlimEndpoints.AOT.Generator
{
    /// <param name="Namespace"> The namespace found in the base struct </param>
    /// <param name="Usings"> Usings of the base struct </param>
    /// <param name="AllowNulls"> Struct allow nulls </param>
    /// <param name="Name"> The short name of the base struct </param>
    /// <param name="NameTyped"> The shot name with typed parameters e.g. <T0, T1>. </param>
    /// <param name="FullName"> The full name of the base struct </param>
    /// <param name="Modifiers"> All modifiers of the base struct e.g. public readonly </param>
    /// <param name="RequestType"> Inner type </param>
    /// <param name="ResponseType"> Base Inner type </param>
    /// <param name="AdditionalConverters"> Array of additional converters </param>
    public record Metadata(string Namespace,
                           IReadOnlyList<string> Usings,
                           bool AllowNulls,
                           string Name,
                           string NameTyped,
                           string FullName,
                           string Modifiers,
                           string RequestType,
                           string ResponseType,
                           string? RequestTypeKind,
                           bool IsRequestTypePositionRecord,
                           List<TypeProperty>? RequestTypeProperties,
                           string Route,
                           string[] Verbs,
                           string Group,
                           string PropertiesWithTypeAndAnnotations,
                           string PropertiesWithType,
                           string PropertiesNames,
                           string PropertiesParse,
                           string PropertiesFromContext,
                           bool IsRequestFromBody,
                           bool IsRequestAsParameter,
                           string ParseinnerBodyRequest,
                           string RecordParametersBodyRequest,
                           string? AuxiliarBodyRequestClassName,
                           bool CreateAuxiliarBodyRequestClass)
    {
        /// <summary>
        /// The namespace found in the base struct
        /// </summary>
        public string Namespace { get; internal set; } = Namespace;

        /// <summary>
        /// Usings of the base struct
        /// </summary>
        public IReadOnlyList<string> Usings { get; internal set; } = Usings;

        /// <summary>
        /// Struct allow nulls
        /// </summary>
        public bool AllowNulls { get; internal set; } = AllowNulls;

        /// <summary>
        /// The short name of the base struct
        /// </summary>
        public string Name { get; internal set; } = Name;

        /// <summary>
        /// The shot name with typed parameters e.g. <T0, T1>.
        /// </summary>
        public string NameTyped { get; internal set; } = NameTyped;

        /// <summary>
        /// The full name of the base struct
        /// </summary>
        public string FullName { get; internal set; } = FullName;

        /// <summary>
        /// All modifiers of the base struct e.g. public readonly
        /// </summary>
        public string Modifiers { get; internal set; } = Modifiers;

        /// <summary>
        /// All types of the ComplexTypes configured
        /// </summary>
        public string RequestType { get; internal set; } = RequestType;

        /// <summary>
        /// All base types of the ComplexTypes configured
        /// </summary>
        public string ResponseType { get; internal set; } = ResponseType;

        public string? RequestTypeKind { get; internal set; } = RequestTypeKind;

        public bool IsRequestTypePositionRecord { get; internal set; } = IsRequestTypePositionRecord;

        public List<TypeProperty>? RequestTypeProperties { get; internal set; } = RequestTypeProperties;

        internal bool IsRequestTypePrimitiveOrId() => GeneratorHelpers.IsTypePrimitiveOrId(RequestType);

        public string Route { get; internal set; } = Route;
        public string[] Verbs{ get; internal set; } = Verbs;
        public string Group { get; internal set; } = Group;

        public string PropertiesWithTypeAndAnnotations { get; internal set; } = PropertiesWithTypeAndAnnotations;
        public string PropertiesWithType { get; internal set; } = PropertiesWithType;
        public string PropertiesNames { get; internal set; } = PropertiesNames;
        public string PropertiesParse { get; internal set; } = PropertiesParse;
        public string PropertiesFromContext { get; internal set; } = PropertiesFromContext;
        public bool IsRequestFromBody { get; internal set; } = IsRequestFromBody;
        public bool IsRequestAsParameter { get; internal set; } = IsRequestAsParameter;
        public string ParseinnerBodyRequest { get; internal set; } = ParseinnerBodyRequest;
        public string RecordParametersBodyRequest { get; internal set; } = RecordParametersBodyRequest;
        public string? AuxiliarBodyRequestClassName { get; internal set; } = AuxiliarBodyRequestClassName;
        public bool CreateAuxiliarBodyRequestClass { get; internal set; } = CreateAuxiliarBodyRequestClass;
    }

}
