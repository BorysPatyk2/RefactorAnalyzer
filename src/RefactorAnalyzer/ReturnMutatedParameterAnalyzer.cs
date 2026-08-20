using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RefactorAnalyzer;

/// <summary>
/// Reports supported <see langword="void"/> methods that directly mutate their sole parameter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReturnMutatedParameterAnalyzer : DiagnosticAnalyzer
{
    /// <summary>The diagnostic identifier emitted by this analyzer.</summary>
    public const string DiagnosticId = DiagnosticDescriptors.ReturnMutatedParameterId;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ReturnMutatedParameter);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        if (methodDeclaration.Body is null ||
            methodDeclaration.ParameterList.Parameters.Count != 1 ||
            methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword) ||
            methodDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) ||
            methodDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
            methodDeclaration.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
            methodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
        {
            return;
        }

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);
        if (methodSymbol is null ||
            methodSymbol.MethodKind != MethodKind.Ordinary ||
            !methodSymbol.ReturnsVoid ||
            methodSymbol.Parameters.Length != 1 ||
            methodSymbol.Parameters[0].RefKind != RefKind.None ||
            !methodSymbol.Parameters[0].Type.IsReferenceType ||
            methodSymbol.ContainingType.TypeKind == TypeKind.Interface ||
            methodSymbol.ExplicitInterfaceImplementations.Length != 0 ||
            methodSymbol.PartialDefinitionPart is not null ||
            methodSymbol.PartialImplementationPart is not null ||
            ImplementsInterfaceMember(methodSymbol))
        {
            return;
        }

        var bodyNodes = GetMethodBodyNodes(methodDeclaration.Body);
        if (bodyNodes.OfType<ReturnStatementSyntax>().Any())
        {
            return;
        }

        var parameter = methodSymbol.Parameters[0];
        var hasDirectMutation = bodyNodes
            .OfType<AssignmentExpressionSyntax>()
            .Any(assignment => IsDirectMutation(assignment, parameter, context.SemanticModel, context.CancellationToken));

        if (!hasDirectMutation)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ReturnMutatedParameter,
            methodDeclaration.Identifier.GetLocation(),
            methodSymbol.Name));
    }

    private static IEnumerable<SyntaxNode> GetMethodBodyNodes(BlockSyntax body) =>
        body.DescendantNodes(descendIntoChildren: static node =>
            node is not AnonymousFunctionExpressionSyntax &&
            node is not LocalFunctionStatementSyntax);

    private static bool IsDirectMutation(
        AssignmentExpressionSyntax assignment,
        IParameterSymbol parameter,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) ||
            assignment.Left is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        var receiver = UnwrapDirectReceiver(memberAccess.Expression);
        if (receiver is not IdentifierNameSyntax identifier ||
            !SymbolEqualityComparer.Default.Equals(
                semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol,
                parameter))
        {
            return false;
        }

        var mutatedMember = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
        return mutatedMember switch
        {
            IFieldSymbol field => !field.IsStatic,
            IPropertySymbol property => !property.IsStatic,
            _ => false,
        };
    }

    private static ExpressionSyntax UnwrapDirectReceiver(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;
                case PostfixUnaryExpressionSyntax suppression
                    when suppression.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                    expression = suppression.Operand;
                    continue;
                default:
                    return expression;
            }
        }
    }

    private static bool ImplementsInterfaceMember(IMethodSymbol method)
    {
        foreach (var interfaceType in method.ContainingType.AllInterfaces)
        {
            foreach (var member in interfaceType.GetMembers(method.Name).OfType<IMethodSymbol>())
            {
                if (SymbolEqualityComparer.Default.Equals(
                        method.ContainingType.FindImplementationForInterfaceMember(member),
                        method))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
