using System;

namespace SlimEndpoints.AOT;

[AttributeUsage(AttributeTargets.Class)]
public class SlimEndpointPipelineAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}