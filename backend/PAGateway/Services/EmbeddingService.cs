using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace PAGateway.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _client;

        public EmbeddingService(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new Uri("http://localhost:11434"); 
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var requestBody = new
            {
                model = "embeddinggemma",
                prompt = text
            };

            var response = await _client.PostAsJsonAsync("/api/embeddings", requestBody);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var embeddingJson = doc.RootElement.GetProperty("embedding").EnumerateArray();
			var embedding = embeddingJson.Select(e => e.GetDouble()).Select(d => (float)d).ToArray();
			return embedding;
		}
    }
}
