using Neo4j.Driver;
using PAGateway.Models;

namespace PAGateway.Services
{
    public class RagService : IAsyncDisposable
    {
        private readonly IDriver _driver;

        public RagService(string uri, string user, string password)
        {
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        }

        // Add a document
        public async Task AddDocumentAsync(Document doc)
        {
            var session = _driver.AsyncSession();
            try
            {
                var embeddingArray = doc.Embedding.Cast<object>().ToArray();
                await session.RunAsync(
                    @"CREATE (d:Document {
                        id:$id,
                        text:$text,
                        embedding:$embedding,
                        created_at:$created_at
                    })",
                    new
                    {
                        id = doc.Id,
                        text = doc.Text,
                        embedding = embeddingArray,
                        created_at = doc.CreatedAt
                    });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        // Query top documents by cosine similarity
        public async Task<List<Document>> QueryAsync(float[] queryEmbedding, int top = 3)
        {
            var session = _driver.AsyncSession();
            var results = new List<Document>();
            try
            {
                var cursor = await session.RunAsync(
                    "MATCH (d:Document) RETURN d.id AS id, d.text AS text, d.embedding AS embedding"
                );
                var records = await cursor.ToListAsync();

                foreach (var record in records)
                {
                    float[] embedding;
                    var embeddingValue = record["embedding"];

                    try
                    {
                        var embeddingList = embeddingValue.As<IList<object>>();
                        embedding = embeddingList.Select(o => Convert.ToSingle(o)).ToArray();
                    }
                    catch (InvalidCastException)
                    {
                        try
                        {
                            var embeddingString = embeddingValue.As<string>();
                            embedding = embeddingString.Split(',')
                                .Select(s => float.Parse(s.Trim()))
                                .ToArray();
                        }
                        catch
                        {
                            try
                            {
                                embedding = embeddingValue.As<float[]>();
                            }
                            catch
                            {
                                throw new InvalidOperationException($"Cannot convert embedding value to float array. Value: {embeddingValue}");
                            }
                        }
                    }

                    results.Add(new Document
                    {
                        Id = record["id"].As<string>(),
                        Text = record["text"].As<string>(),
                        Embedding = embedding
                    });
                }

                // Rank by cosine similarity
                return results
                    .Select(d => new { Doc = d, Score = CosineSimilarity(d.Embedding, queryEmbedding) })
                    .OrderByDescending(x => x.Score)
                    .Take(top)
                    .Select(x => x.Doc)
                    .ToList();
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same length");

            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            var magnitude = MathF.Sqrt(magA) * MathF.Sqrt(magB);
            return magnitude == 0 ? 0 : dot / magnitude;
        }

        public async ValueTask DisposeAsync()
        {
            await _driver.CloseAsync();
        }
    }
}