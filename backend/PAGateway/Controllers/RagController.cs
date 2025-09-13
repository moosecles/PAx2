using Microsoft.AspNetCore.Mvc;
using PAGateway.Models;
using PAGateway.Services;

namespace PAGateway.Controllers
{
    [ApiController]
    [Route("api/rag")]
    public class RagController : ControllerBase
    {
        private readonly RagService _rag;
        private readonly EmbeddingService _embeddingService;

        public RagController(RagService rag, EmbeddingService embeddingService)
        {
            _rag = rag;
            _embeddingService = embeddingService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] RagRequest request)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Text);

            var doc = new Document
            {
                Text = request.Text,
                Embedding = embedding
            };

            await _rag.AddDocumentAsync(doc);
            return Ok(doc);
        }

        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] RagRequest request)
        {
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Text);
            var results = await _rag.QueryAsync(queryEmbedding);
            return Ok(results);
        }

    }
}
