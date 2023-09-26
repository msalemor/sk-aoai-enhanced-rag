namespace server.Services;
public class ExtractTextBaseService<T> : IExtractText<T>
{
    private readonly FileUtilityService service;

    public ExtractTextBaseService(FileUtilityService service)
    {
        this.service = service;
    }

    virtual public Task<string> ExtractTextFromFileAsync(string filePath)
    {
        throw new NotImplementedException();
    }

    virtual public async Task<string> ExtractTextFromUrl(HttpClient client, string url, string folder)
    {
        var filePath = await service.DownloadFileFromUrlAsync(client, folder, url);
        string content;
        try { content = await ExtractTextFromFileAsync(filePath); }
        finally
        {
            File.Delete(filePath);
        }
        return content;
    }

}