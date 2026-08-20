using Microsoft.CodeAnalysis;

namespace RefactorAnalyzer;

internal static class DiagnosticDescriptors
{
    public const string ReturnMutatedParameterId = "RA0001";

    public static readonly DiagnosticDescriptor ReturnMutatedParameter = new(
        ReturnMutatedParameterId,
        "Return the mutated parameter",
        "Method '{0}' mutates its parameter and can return it",
        "Design",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A void method that directly mutates its sole reference-type parameter can return that parameter.");
}
