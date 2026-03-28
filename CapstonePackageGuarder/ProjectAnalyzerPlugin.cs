using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CapstonePackageGuarder;

public class ProjectAnalyzerPlugin
{
    private readonly ISemanticTextMemory _memory;

    public ProjectAnalyzerPlugin(ISemanticTextMemory memory)
    {
        _memory = memory;
    }

    [KernelFunction("find_projects")]
    [Description("Finds all .csproj file paths in a given directory. Use this to discover projects before inspecting them.")]
    public string FindProjects([Description("Absolute or relative directory path to search in")] string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return $"Error: Directory not found at {directoryPath}";

        try
        {
            var files = Directory.GetFiles(directoryPath, "*.csproj", SearchOption.AllDirectories);
            if (files.Length == 0) return "No .csproj files found in the directory.";
            return string.Join("\n", files);
        }
        catch (Exception ex)
        {
            return $"Error searching directory: {ex.Message}";
        }
    }

    [KernelFunction("get_project_packages")]
    [Description("Gets a list of all NuGet packages from a given .csproj file path, and automatically checks the Vulnerability Knowledge Base (RAG) for security issues.")]
    public async Task<string> GetProjectPackages([Description("Absolute or relative path to the .csproj file")] string projectPath)
    {
        if (!File.Exists(projectPath))
            return $"Error: File not found at {projectPath}";

        try
        {
            var content = File.ReadAllText(projectPath);
            var doc = System.Xml.Linq.XDocument.Parse(content);
            var packageReferences = doc.Descendants().Where(e => e.Name.LocalName == "PackageReference").ToList();

            if (packageReferences.Count == 0) return "No NuGet packages found in the project.";

            var results = new List<string>();
            foreach (var pr in packageReferences)
            {
                string packageName = pr.Attribute("Include")?.Value ?? "";
                string version = pr.Attribute("Version")?.Value ?? "";
                if (string.IsNullOrEmpty(packageName)) continue;
                
                // RAG Lookup - Increased sensitivity and added direct substring check to avoid false positives (e.g., Microsoft.Extensions vs AutoMapper)
                string ragContext = "No known vulnerabilities in DB.";
                try
                {
                    await foreach (var memoryResult in _memory.SearchAsync("vulnerabilities", packageName, limit: 1, minRelevanceScore: 0.5))
                    {
                        if (memoryResult.Metadata.Text.Contains(packageName, StringComparison.OrdinalIgnoreCase))
                        {
                            ragContext = $"[SECURITY WARNING from KB: {memoryResult.Metadata.Text}]";
                            break;
                        }
                    }
                }
                catch (Exception) { /* Skip if embedding hits rate limit temporarily, just provide the version */ }

                results.Add($"- Package: {packageName} | Version: {version} | RAG Check: {ragContext}");
            }
            return string.Join("\n", results);
        }
        catch (Exception ex)
        {
            return $"Error reading project file: {ex.Message}";
        }
    }

    [KernelFunction("update_project_package")]
    [Description("Updates a specific NuGet package to a target version in the given project by running 'dotnet add package'. WARNING: Make sure you got the permission from the user to do this.")]
    public string UpdateProjectPackage(
        [Description("Path to the .csproj file to update")] string projectPath,
        [Description("Name of the NuGet package to update")] string packageName,
        [Description("Target secure version of the package")] string targetVersion)
    {
        if (!File.Exists(projectPath))
            return $"Error: Project file not found at {projectPath}";

        try
        {
            var process = new Process();
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = $"add \"{projectPath}\" package {packageName} -v {targetVersion}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string err = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
                return $"Successfully updated {packageName} to version {targetVersion}.";
            else
                return $"Failed to update {packageName}. Error:\n{err}";
        }
        catch (Exception ex)
        {
            return $"Error executing update command: {ex.Message}";
        }
    }
}
