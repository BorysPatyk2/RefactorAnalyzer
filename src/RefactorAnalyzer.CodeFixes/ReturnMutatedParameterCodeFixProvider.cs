using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace RefactorAnalyzer;

/// <summary>
/// Changes a supported method to return its parameter after mutating it.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReturnMutatedParameterCodeFixProvider)), Shared]
public sealed class ReturnMutatedParameterCodeFixProvider : CodeFixProvider
{
    private const string Title = "Return the mutated parameter";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ReturnMutatedParameterAnalyzer.DiagnosticId);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var methodDeclaration = root?
            .FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent?
            .FirstAncestorOrSelf<MethodDeclarationSyntax>();

        if (methodDeclaration?.Body is null || methodDeclaration.ParameterList.Parameters.Count != 1)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => ApplyFixAsync(context.Document, methodDeclaration, cancellationToken),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> ApplyFixAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var parameter = methodDeclaration.ParameterList.Parameters[0];
        if (parameter.Type is null || methodDeclaration.Body is null)
        {
            return document;
        }

        var returnType = parameter.Type.WithTriviaFrom(methodDeclaration.ReturnType);
        var returnStatement = SyntaxFactory.ReturnStatement(
                SyntaxFactory.IdentifierName(parameter.Identifier.WithoutTrivia()))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var updatedMethod = methodDeclaration
            .WithReturnType(returnType)
            .WithBody(methodDeclaration.Body.AddStatements(returnStatement))
            .WithAdditionalAnnotations(Formatter.Annotation);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return root is null
            ? document
            : document.WithSyntaxRoot(root.ReplaceNode(methodDeclaration, updatedMethod));
    }
}
