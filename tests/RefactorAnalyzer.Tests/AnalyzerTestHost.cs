using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace RefactorAnalyzer.Tests;

internal static class AnalyzerTestHost
{
    public static async Task<(Document Document, ImmutableArray<Diagnostic> Diagnostics)> AnalyzeAsync(string source)
    {
        var document = CreateDocument(source);
        var compilation = await document.Project.GetCompilationAsync();
        Assert.NotNull(compilation);
        Assert.Empty(compilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ReturnMutatedParameterAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();

        return (document, diagnostics);
    }

    public static async Task<string> ApplyCodeFixAsync(string source)
    {
        var (document, diagnostics) = await AnalyzeAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        var actions = new List<CodeAction>();
        var provider = new ReturnMutatedParameterCodeFixProvider();

        var context = new CodeFixContext(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);
        await provider.RegisterCodeFixesAsync(context);

        var action = Assert.Single(actions);
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var applyChanges = Assert.Single(operations.OfType<ApplyChangesOperation>());
        var changedDocument = applyChanges.ChangedSolution.GetDocument(document.Id);
        Assert.NotNull(changedDocument);

        var changedCompilation = await changedDocument!.Project.GetCompilationAsync();
        Assert.NotNull(changedCompilation);
        Assert.Empty(changedCompilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        return (await changedDocument.GetTextAsync()).ToString();
    }

    public static string Normalize(string source) =>
        CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().ToFullString();

    private static Document CreateDocument(string source)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "TestProject",
                "TestProject",
                LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest)))
            .AddMetadataReferences(projectId, GetPlatformReferences())
            .AddDocument(documentId, "Test.cs", SourceText.From(source));

        return solution.GetDocument(documentId)!;
    }

    private static IEnumerable<MetadataReference> GetPlatformReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        Assert.False(string.IsNullOrWhiteSpace(trustedAssemblies));

        return trustedAssemblies!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
