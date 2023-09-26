using System.Text;
using System.Web;
using dotenv.net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Memory.Sqlite;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticKernel.Text;
using server.Repositories;
using server.Services;
using System.Net;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using server.Models;

// https://alemoraoaist.z13.web.core.windows.net/docs/Contoso-Company_Benefits.pdf
// https://alemoraoaist.z13.web.core.windows.net/docs/Contoso-401K_Policy.pdf
// https://alemoraoaist.z13.web.core.windows.net/docs/Contoso-Medical_Leave_Policy.pdf
// https://alemoraoaist.z13.web.core.windows.net/docs/Contoso-Resignation_Policy.pdf

const string DOC_COLLECTION = "docs";
const string BLOC_COLLECTION = "blob";
const int MAX_CHUNK_SIZE = 512;

var builder = WebApplication.CreateBuilder(args);

#region Read amd validate the environment variables

var appConfig = new AppConfigService();

#endregion

#region Get an application builder and configure Semantic Kernel

var connectionString = "Data Source=" + appConfig.DB_PATH;
var sqlDbUtil = SqliteDbService.GetInstance(connectionString);
await sqlDbUtil.CreateTableAsync(DocSqliteRepository.TABLE_DEFINITION);
builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(sqlDbUtil);
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRepository<Doc>, DocSqliteRepository>();
builder.Services.AddSingleton(new SkService());
builder.Services.AddSingleton(new FileUtilityService());
builder.Services.AddSingleton(new TextUtilityService());
builder.Services.AddSingleton<IExtractText<PdfFileType>, PdfExtractService>();
builder.Services.AddSingleton<IExtractText<TextFileType>, TextFileExtractService>();

// Configure Semantic Kernel
var sqliteStore = await SqliteMemoryStore.ConnectAsync(appConfig.DB_PATH);
IKernel kernel = new KernelBuilder()
    .WithAzureChatCompletionService(appConfig.gptDeploymentName, appConfig.endpoint, appConfig.apiKey)
    .WithAzureTextEmbeddingGenerationService(appConfig.adaDeploymentName, appConfig.endpoint, appConfig.apiKey)
    .WithMemoryStorage(sqliteStore)
    .Build();
var memorySkill = new TextMemorySkill(kernel.Memory);
kernel.ImportSkill(memorySkill);

#endregion

#region Configure the application and build it

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opts => opts.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
//app.UseAuthorization();

#endregion

#region Summarizer route

/// <summary>
/// Breaks text into chunks of a given size.
/// </summary>
/// <param name="content">The text to break into chunks.</param>
/// <param name="chunk_size">The size of each chunk.</param>
/// <returns>A list of chunks.</returns>
List<string> ChunkText(string content, int chunk_size)
{
    var lines = TextChunker.SplitPlainTextLines(content, chunk_size / 2);
    // return paragraphs
    return TextChunker.SplitPlainTextParagraphs(lines, chunk_size);
}

/// <summary>
/// Summarizes text.
/// </summary>
/// <param name="prompt">The prompt to use for the completion.</param>
/// <param name="content">The text to summarize.</param>
/// <param name="chunk_size">The size of each chunk.</param>
/// <param name="max_tokens">The maximum number of tokens to generate.</param>
/// <param name="temperature">The temperature to use for the completion.</param>
/// <returns>A completion response.</returns>
app.MapPost("/api/summarize", async ([FromBody] SummarizeRequest request) =>
{
    var prompt = string.Empty;
    ISKFunction? fixedFunction;
    SKContext? result;
    var summaries = new List<Summary>();

    if (request.chunk_size == 0 || request.temperature == 0 || request.max_tokens == 0 || string.IsNullOrEmpty(request.prompt))
    {
        return Results.BadRequest();
    }

    // Step 1: See if it is a simple prompt, if so just return the completion
    // TODO: review logic when there's a template 
    //   if (string.IsNullOrEmpty(request.content) || request.content.IndexOf("<TEXT>") == -1)
    if (string.IsNullOrEmpty(request.content))
    {
        fixedFunction = kernel.CreateSemanticFunction(request.prompt, maxTokens: request.max_tokens, temperature: request.temperature);
        result = await kernel.RunAsync(fixedFunction);
        return Results.Ok(new CompletionResponse(result.ToString(), summaries));
    }

    // Step 2: apply the prompt to each chunk
    var chunks = ChunkText(request.content, request.chunk_size);
    var chunkCompletions = new List<string>();
    foreach (var chunk in chunks)
    {
        prompt = request.prompt.Replace("<TEXT>", chunk);
        fixedFunction = kernel.CreateSemanticFunction(prompt, maxTokens: request.max_tokens, temperature: request.temperature);

        // TODO: This could run concurrently for increased performance, but doing it sequentially to spare resources
        // Keep a list of tasks: List<Task<SKContext>> tasks = new();
        // Instead of await run all tasks: tasks.Add(kernel.RunAsync(fixedFunction));
        // Wait for all tasks to complete: Task.WhenAll(tasks)
        // Get the results from all tasks: tasks[0].Result, tasks[1].Result, ...

        result = await kernel.RunAsync(fixedFunction);
        chunkCompletions.Add(result.ToString());
    }

    // If there's only one chunk, don't do further processing and return the result
    if (chunks.Count == 1)
    {
        summaries.Clear();
        summaries = new List<Summary>
        {
            new(request.content, chunkCompletions[0])
        };
        return Results.Ok(new CompletionResponse(chunkCompletions[0], summaries));
    }

    // Step 3: Combine the chunk results
    var sb = new StringBuilder();
    foreach (var content in chunkCompletions)
    {
        sb.Append(content + "\n\n");
    }

    // Step 4: Apply the prompt to the combined chunk results
    prompt = request.prompt.Replace("<TEXT>", sb.ToString());
    fixedFunction = kernel.CreateSemanticFunction(prompt, maxTokens: request.max_tokens, temperature: request.temperature);
    result = await kernel.RunAsync(fixedFunction);

    // Step 5: return the completion
    summaries.Clear();
    for (var i = 0; i < chunks.Count; i++)
    {
        summaries.Add(new(chunks[i], chunkCompletions[i]));
    }
    return Results.Ok(new CompletionResponse(result.ToString(), summaries));
});
#endregion

#region Docs

app.MapPost("/api/doc/ingest/{url}", async (HttpClient client,
    string? url,
    IRepository<Doc> repository,
    SkService service,
    IExtractText<TextFileType> textFileService,
    IExtractText<PdfFileType> pdfFileService
     ) =>
{
    url = HttpUtility.UrlDecode(url);
    if (string.IsNullOrEmpty(url))
        return Results.BadRequest();

    var fileName = Path.GetFileName(url);
    var ext = Path.GetExtension(fileName).ToLower();
    string content = string.Empty;
    if (ext == ".pdf")
    {
        // Process PDF
        content = await pdfFileService.ExtractTextFromUrl(client, url, appConfig.TMP_DOC_FOLDER);
    }
    else if (ext == ".txt" || ext == ".md")
    {
        // Process a Text File
        content = await textFileService.ExtractTextFromUrl(client, url, appConfig.TMP_DOC_FOLDER);
    }
    else
    {
        return Results.BadRequest();
    }

    var memoryRecords = new List<Memory>();
    if (!string.IsNullOrEmpty(content))
    {
        await repository.UpsertAsync(DOC_COLLECTION, fileName, fileName, url);
        var chunks = ChunkText(content, MAX_CHUNK_SIZE);
        var totalChunks = chunks.Count;

        for (var i = 0; i < totalChunks; i++)
        {
            var record = new Memory(BLOC_COLLECTION, $"{fileName}-{totalChunks}-{i + 1}", chunks[i]);
            memoryRecords.Add(record);
            await service.SkSaveMemoryAsync(kernel, record, memorySkill);
        }
    }

    return Results.Ok(new { url, memoryRecords });
});

app.MapGet("/api/doc/{collection}", async (string collection, IRepository<Doc> repository) =>
{
    var docs = await repository.GetAllDocsAsync(collection);
    if (docs is null || docs.Count == 0)
    {
        return Results.NotFound();
    }
    return Results.Ok(docs);
})
.WithName("GetDocs")
.WithOpenApi();

app.MapGet("/api/doc/{collection}/{key}", async (string collection, string key, IRepository<Doc> repository) =>
{
    var doc = await repository.GetAsync(collection, key);
    if (doc is null)
    {
        return Results.NotFound();
    }
    return Results.Ok(doc);
})
.WithName("GetDoc")
.WithOpenApi();

app.MapPost("/api/doc", async ([FromBody] Doc doc, IRepository<Doc> repository) =>
{
    var insertedDoc = await repository.UpsertAsync(doc.collection, doc.key, doc.description, doc.location);
    return Results.Created($"/api/gpt/doc/{insertedDoc.collection}/{insertedDoc.key}", insertedDoc);
})
.WithName("PostDoc")
.WithOpenApi();

app.MapDelete("/api/doc/{collection}/{key}", async (string collection, string key, IRepository<Doc> repository) =>
{
    var affected = await repository.DeleteAsync(collection, key);
    if (affected == 0)
    {
        return Results.NotFound();
    }
    return Results.Ok(new { collection, key });
})
.WithName("DeleteDoc")
.WithOpenApi();

#endregion

#region RAG pattern routes

// Routes
//"benefits.pdf-1..10"
app.MapGet("/api/gpt/memory/{collection}/{id}", async (string collection, string key, SkService service) =>
{
    var result = await service.SkMemoryGetAsync(collection, key, memorySkill);
    if (string.IsNullOrEmpty(result))
    {
        return Results.NotFound();
    }
    var memoryResponse = new Memory(collection, key, result);
    return Results.Ok(memoryResponse);
})
.WithName("GetMemory")
.WithOpenApi();

// Note: It is up to the calling application to implement the text extraction and chunking logic
// Note: Pass a document name with a content
app.MapPost("/api/gpt/memory", async ([FromBody] Memory memory, SkService service) =>
{
    var result = await service.SkSaveMemoryAsync(kernel, memory, memorySkill);
    if (string.IsNullOrEmpty(result))
    {
        return Results.BadRequest();
    }
    return Results.Ok(memory);
})
.WithName("PostMemory")
.WithOpenApi();

app.MapDelete("/api/gpt/memory", async ([FromBody] Memory memory, SkService service) =>
{
    var result = await service.SkDeleteMemoryAsync(kernel, memory);
    if (!result)
    {
        return Results.BadRequest();
    }
    return Results.Ok(memory);
})
.WithName("DeleteMemory")
.WithOpenApi();

app.MapPost("/api/gpt/query", async ([FromBody] Query query, SkService service) =>
{
    var completion = await service.SkQueryAsync(kernel, query);
    return Results.Ok(completion);
})
.WithName("PostQuery")
.WithOpenApi();

#endregion

#region Run the application

app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.Run();

#endregion

public class AppConfigService
{
    public string gptDeploymentName { get; private set; }

    public string adaDeploymentName { get; private set; }
    public string endpoint { get; private set; }
    public string apiKey { get; private set; }
    public string DB_PATH { get; private set; }
    public string TMP_DOC_FOLDER { get; private set; }

    public AppConfigService()
    {
        DotEnv.Load();

        gptDeploymentName = Environment.GetEnvironmentVariable("GPT_DEPLOYMENT_NAME") ?? "";
        adaDeploymentName = Environment.GetEnvironmentVariable("ADA_DEPLOYMENT_NAME") ?? "";
        endpoint = Environment.GetEnvironmentVariable("ENDPOINT") ?? "";
        apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? "";
        DB_PATH = Environment.GetEnvironmentVariable("SQLITE_DB_PATH") ?? "./data/db/vectors.sqlite";
        TMP_DOC_FOLDER = Environment.GetEnvironmentVariable("TMP_DOC_PATH") ?? "./data/docs/";

        if (string.IsNullOrEmpty(gptDeploymentName) || string.IsNullOrEmpty(adaDeploymentName) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(adaDeploymentName) || string.IsNullOrEmpty(DB_PATH))
        {
            Console.WriteLine("Please set the following environment variables: GPT_DEPLOYMENT_NAME, ADA_DEPLOYMENT_NAME, ENDPOINT, API_KEY, SQLITE_DB_PATH");
            Environment.Exit(1);
        }
    }

}