using Xunit;

namespace RefactorAnalyzer.Tests;

public sealed class ReturnMutatedParameterAnalyzerTests
{
    [Theory]
    [InlineData("user.Name = \"Ala\";")]
    [InlineData("user.Age = 42;")]
    public async Task ReportsDirectAssignmentToPropertyOrField(string assignment)
    {
        var source = $$"""
            class User
            {
                public string Name { get; set; } = "";
                public int Age;
            }

            class Service
            {
                void Update(User user)
                {
                    {{assignment}}
                }
            }
            """;

        var (_, diagnostics) = await AnalyzerTestHost.AnalyzeAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ReturnMutatedParameterAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Equal("Update", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
    }

    [Theory]
    [MemberData(nameof(UnsupportedMethods))]
    public async Task DoesNotReportUnsupportedMethods(string method)
    {
        var source = $$"""
            class User
            {
                public string Name { get; set; } = "";
            }

            class Service
            {
                {{method}}
            }
            """;

        var (_, diagnostics) = await AnalyzerTestHost.AnalyzeAsync(source);

        Assert.Empty(diagnostics);
    }

    public static TheoryData<string> UnsupportedMethods => new()
    {
        "User Update(User user) { user.Name = \"Ala\"; return user; }",
        "void Update() { }",
        "void Update(User user, User other) { user.Name = \"Ala\"; }",
        "void Update(int value) { value = 1; }",
        "void Update(UserValue user) { user.Name = \"Ala\"; } struct UserValue { public string Name; }",
        "void Update(ref User user) { user.Name = \"Ala\"; }",
        "void Update(out User user) { user = new User(); user.Name = \"Ala\"; }",
        "void Update(in User user) { user.Name = \"Ala\"; }",
        "void Update(User user) { if (user is null) return; user.Name = \"Ala\"; }",
        "void Update(User user) { SetName(user); } void SetName(User user) { }",
        "void Update(User user) { user.Name += \"!\"; }",
        "void Update(User user) => user.Name = \"Ala\";",
        "async void Update(User user) { await System.Threading.Tasks.Task.Yield(); user.Name = \"Ala\"; }",
        "void Update(User user) { System.Action change = () => user.Name = \"Ala\"; }",
        "void Update(User user) { void Change() { user.Name = \"Ala\"; } }",
    };
}
