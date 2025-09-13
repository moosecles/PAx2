using PAGateway.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

var dbUri = Environment.GetEnvironmentVariable("DB_URI");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPass = Environment.GetEnvironmentVariable("DB_PASS");


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<RagService>(sp => new RagService(dbUri, dbUser, dbPass)); 
builder.Services.AddHttpClient<EmbeddingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers(); 

app.Run();
