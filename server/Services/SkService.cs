using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Skills.Core;
using server.Models;

namespace server.Services;

public class SkService
{
    public string DocPath { get; set; } = null!;
    public async Task<string> SkMemoryGetAsync(string collection, string key, TextMemorySkill memorySkill)
    {
        return await memorySkill.RetrieveAsync(collection, key, null);
    }

    public async Task<string> SkSaveMemoryAsync(IKernel kernel, Memory memory, TextMemorySkill memorySkill)
    {
        var skMemory = await memorySkill.RetrieveAsync(memory.collection, memory.key, null);
        if (skMemory is not null)
        {
            await kernel.Memory.RemoveAsync(memory.collection, memory.key);
        }
        var parts = memory.key.Split("-");
        var jsonObj = new { docID = parts[0] };
        var json = JsonSerializer.Serialize(jsonObj);
        return await kernel.Memory.SaveInformationAsync(memory.collection, memory.text, memory.key, parts[0], json);
    }

    public async Task<bool> SkDeleteMemoryAsync(IKernel kernel, Memory memory)
    {
        try
        {
            await kernel.Memory.RemoveAsync(memory.collection, memory.key);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return false;
    }

    public async Task<Completion> SkQueryAsync(IKernel kernel, Query query)
    {
        const string BLOC_COLLECTION = "blob";
        string collection = BLOC_COLLECTION;
        if (query.collection is not null)
        {
            collection = query.collection;
        }
        IAsyncEnumerable<MemoryQueryResult> queryResults =
            kernel.Memory.SearchAsync(collection, query.query, limit: query.limit, minRelevanceScore: query.minRelevanceScore);

        var promptData = new StringBuilder();
        var learnMore = new List<LearnMore>();
        await foreach (MemoryQueryResult r in queryResults)
        {
            promptData.Append(r.Metadata.Text + "\n\n");
            var key = r.Metadata.Id;
            var name = key.Replace("Contoso-", "").Replace("_", " ");
            name = name.Substring(0, name.IndexOf("."));

            if (learnMore.Exists(c => c.name == name))
            {
                continue;
            }
            learnMore.Add(new LearnMore(collection, r.Metadata.Id, name));
        }

        if (promptData.Length == 0)
        {
            return new Completion(query.query, "I cannot get the answer at this time.", null, learnMore);
        }

        const string ragFunctionDefinition = "{{$input}}\n\nText:\n\"\"\"{{$data}}\n\"\"\"Use only the provided text. If you cannot get the answer from the provided data, say \"I cannot get the answer at this time.\"";
        var ragFunction = kernel.CreateSemanticFunction(ragFunctionDefinition, maxTokens: query.maxTokens);
        var result = await kernel.RunAsync(ragFunction, new(query.query)
        {
            ["data"] = promptData.ToString()
        });

        return new Completion(query.query, result.ToString(), result.ModelResults.LastOrDefault()?.GetOpenAIChatResult()?.Usage, learnMore);
    }

    // TODO: Turn SkService into a plugable service
    // public async Task SaveMemories(IKernel kernel, TextMemorySkill memorySkill, IRepository<Doc> repository, TextUtilityService textService, string fileName, string content)
    // {
    //     var memoryRecords = new List<Memory>();
    //     if (!string.IsNullOrEmpty(content))
    //     {
    //         await repository.UpsertAsync(DOC_COLLECTION, fileName, fileName, url);
    //         var chunks = textService.ChunkText(content, MAX_CHUNK_SIZE);
    //         var totalChunks = chunks.Count;

    //         for (var i = 0; i < totalChunks; i++)
    //         {
    //             var record = new Memory(BLOC_COLLECTION, $"{fileName}-{totalChunks}-{i + 1}", chunks[i]);
    //             memoryRecords.Add(record);
    //             await SkSaveMemoryAsync(kernel, record, memorySkill);
    //         }
    //     }
    // }
}