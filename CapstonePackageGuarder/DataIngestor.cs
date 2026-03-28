using Microsoft.SemanticKernel.Memory;

namespace CapstonePackageGuarder;

public class DataIngestor
{
    private readonly ISemanticTextMemory _memory;

    public DataIngestor(ISemanticTextMemory memory)
    {
        _memory = memory;
    }

    public async Task IngestDataAsync(string filePath)
    {
        Console.WriteLine($"Ingesting knowledge base from {filePath}...");
        if (!File.Exists(filePath))
        {
            Console.WriteLine("Knowledge base file not found.");
            return;
        }

        var content = await File.ReadAllTextAsync(filePath);
        
        // Simple chunking by empty lines
        var chunks = content.Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < chunks.Length; i++)
        {
            if (chunks[i].StartsWith("# Vulnerable Packages")) continue;
            
            string id = $"kb_{i}";
            await _memory.SaveInformationAsync(
                collection: "vulnerabilities",
                text: chunks[i],
                id: id,
                description: "Security knowledge base entry"
            );
        }
        Console.WriteLine($"Ingestion complete! ({chunks.Length - 1} chunks saved)");
    }
}
