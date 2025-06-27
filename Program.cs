using McpServer.Configuration;
using McpServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// === Configurazioni esistenti ===
builder.Services.Configure<EmbeddingSettings>(
    builder.Configuration.GetSection("Embedding"));

builder.Services.Configure<EmbeddingFTSettings>(
builder.Configuration.GetSection("EmbeddingFT"));

builder.Services.Configure<EmbeddingFTImageSettings>(
builder.Configuration.GetSection("EmbeddingFTImage"));

builder.Services.Configure<ElasticSettings>(
    builder.Configuration.GetSection("ElasticSearch"));

builder.Services.Configure<SummarizeSettings>(
builder.Configuration.GetSection("Summarize"));

builder.Services.AddDbContext<McpDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.Configure<FtImagesSettings>(builder.Configuration.GetSection("FTImages"));

builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ElasticsearchService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<RenderTextService>();
builder.Services.AddScoped<ElementService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(provider => {
    var config = provider.GetRequiredService<IConfiguration>();
    var uri = config["Neo4j:Uri"];
    var user = config["Neo4j:User"];
    var password = config["Neo4j:Password"];
    return new Neo4jGraphService(uri, user, password);
});

var app = builder.Build();

// === Middleware standard ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

app.MapControllers(); // Attiva le API REST

app.UseStaticFiles();
app.Run();


