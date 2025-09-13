using Microsoft.AspNetCore.Mvc;
using PAGateway.Models;
using PAGateway.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PAGateway.Controllers
{
    [ApiController]
    [Route("api/ollama")]
    public class OllamaController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly RagService _rag;
        private readonly EmbeddingService _embeddingService;

        public OllamaController(
            IHttpClientFactory httpClientFactory,
            RagService rag,
            EmbeddingService embeddingService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _rag = rag ?? throw new ArgumentNullException(nameof(rag));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        }

        public class OllamaRequest
        {
            public string Model { get; set; } = "gemma3:1b";
            public string Prompt { get; set; } = string.Empty;
            public bool Stream { get; set; } = false;
        }

        public class OllamaResponse
        {
            public string Response { get; set; } = string.Empty;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateWithContext([FromBody] RagRequest request)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Text);

            var topDocs = await _rag.QueryAsync(queryEmbedding);

            var context = string.Join("\n", topDocs.Select(d => d.Text));

            var prompt = $"Answer the following question using the context below. If the answer is not in the context, answer based on your knowledge.\n\nContext:\n{context}\n\nQuestion:\n{request.Text}";

            var ollamaRequest = new OllamaController.OllamaRequest
            {
                Prompt = prompt,
                Model = "gemma3:1b",
                Stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(ollamaRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var generatedText = doc.RootElement.GetProperty("response").GetString();

            return Ok(new { Answer = generatedText });
        }

    }
}
