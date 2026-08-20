using Xunit;

namespace RefactorAnalyzer.Tests;

public sealed class ReturnMutatedParameterCodeFixProviderTests
{
    [Fact]
    public async Task ChangesReturnTypeAndAppendsParameterReturn()
    {
        const string source = """
            class User
            {
                public string Name { get; set; } = "";
            }

            class Service
            {
                void Update(User user)
                {
                    user.Name = "Ala";
                }
            }
            """;
        const string expected = """
            class User
            {
                public string Name { get; set; } = "";
            }

            class Service
            {
                User Update(User user)
                {
                    user.Name = "Ala";
                    return user;
                }
            }
            """;

        var fixedSource = await AnalyzerTestHost.ApplyCodeFixAsync(source);

        Assert.Equal(AnalyzerTestHost.Normalize(expected), AnalyzerTestHost.Normalize(fixedSource));
    }

    [Fact]
    public async Task PreservesNullableParameterType()
    {
        const string source = """
            #nullable enable
            class User { public string? Name { get; set; } }
            class Service
            {
                void Update(User? user)
                {
                    user!.Name = "Ala";
                }
            }
            """;
        const string expected = """
            #nullable enable
            class User { public string? Name { get; set; } }
            class Service
            {
                User? Update(User? user)
                {
                    user!.Name = "Ala";
                    return user;
                }
            }
            """;

        var fixedSource = await AnalyzerTestHost.ApplyCodeFixAsync(source);

        Assert.Equal(AnalyzerTestHost.Normalize(expected), AnalyzerTestHost.Normalize(fixedSource));
    }

    [Fact]
    public async Task PreservesEscapedParameterIdentifier()
    {
        const string source = """
            class User { public string Name { get; set; } = ""; }
            class Service
            {
                void Update(User @class)
                {
                    @class.Name = "Ala";
                }
            }
            """;
        const string expected = """
            class User { public string Name { get; set; } = ""; }
            class Service
            {
                User Update(User @class)
                {
                    @class.Name = "Ala";
                    return @class;
                }
            }
            """;

        var fixedSource = await AnalyzerTestHost.ApplyCodeFixAsync(source);

        Assert.Equal(AnalyzerTestHost.Normalize(expected), AnalyzerTestHost.Normalize(fixedSource));
    }
}
