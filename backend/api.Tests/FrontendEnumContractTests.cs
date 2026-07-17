using System.Text.RegularExpressions;
using api.Models.Enums;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Tests;

public sealed class FrontendEnumContractTests
{
    [Fact]
    public void WorkStatus_Values_Match_Angular_Contract()
    {
        var frontendValues = ReadTypeScriptEnum("WorkStatus");
        var backendValues = Enum.GetValues<AgileTaskStatus>()
            .ToDictionary(value => value.ToString(), value => (int)value);

        AssertEnumValuesEqual(backendValues, frontendValues);
    }

    [Fact]
    public void TaskPriority_Values_Match_Angular_Contract()
    {
        var frontendValues = ReadTypeScriptEnum("TaskPriority");
        var backendValues = Enum.GetValues<TaskPriority>()
            .ToDictionary(value => value.ToString(), value => (int)value);

        AssertEnumValuesEqual(backendValues, frontendValues);
    }

    private static void AssertEnumValuesEqual(
        IReadOnlyDictionary<string, int> backendValues,
        IReadOnlyDictionary<string, int> frontendValues)
    {
        Assert.Equal(backendValues.Count, frontendValues.Count);

        foreach (var (name, value) in backendValues)
        {
            Assert.True(frontendValues.TryGetValue(name, out var frontendValue), $"Angular enum is missing {name}.");
            Assert.Equal(value, frontendValue);
        }
    }

    private static Dictionary<string, int> ReadTypeScriptEnum(string enumName)
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourcePath = Path.Combine(
            repositoryRoot,
            "frontend",
            "src",
            "app",
            "core",
            "models",
            "task.models.ts");
        var source = File.ReadAllText(sourcePath);
        var enumMatch = Regex.Match(
            source,
            $@"export\s+enum\s+{Regex.Escape(enumName)}\s*\{{(?<body>[\s\S]*?)\}}",
            RegexOptions.CultureInvariant);

        Assert.True(enumMatch.Success, $"Angular enum {enumName} was not found in {sourcePath}.");

        return Regex.Matches(
                enumMatch.Groups["body"].Value,
                @"(?<name>[A-Za-z][A-Za-z0-9_]*)\s*=\s*(?<value>-?\d+)",
                RegexOptions.CultureInvariant)
            .Cast<Match>()
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => int.Parse(match.Groups["value"].Value));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AgileWorkspace.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Agile Workspace repository root.");
    }
}

