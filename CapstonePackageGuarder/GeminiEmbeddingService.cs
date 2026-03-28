using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace CapstonePackageGuarder;

public class GeminiEmbeddingService : ITextEmbeddingGenerationService
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    public GeminiEmbeddingService(string apiKey, string model = "gemini-embedding-001")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient();
    }

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        var result = new List<ReadOnlyMemory<float>>();
        foreach (var text in data)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:embedContent?key={_apiKey}";
            var payload = new
            {
                content = new { parts = new[] { new { text } } }
            };

            HttpResponseMessage? response = null;
            int retries = 0;
            bool success = false;
            while (!success && retries < 3)
            {
                response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    retries++;
                    Console.WriteLine($"[⏳ API Rate Limit Hit for Embeddings. Waiting 10s... (Attempt {retries}/3)]");
                    await Task.Delay(10000);
                }
                else
                {
                    success = true;
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Gemini Embedding API Error: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var values = json.GetProperty("embedding").GetProperty("values").EnumerateArray();
            var vector = values.Select(x => x.GetSingle()).ToArray();
            result.Add(new ReadOnlyMemory<float>(vector));
        }
        return result;
    }
}
