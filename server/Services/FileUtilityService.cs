namespace server.Services;

public class FileUtilityService
{
    public static void DeleteFileAsync(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch { }
    }

    public async Task<string> DownloadFileFromUrlAsync(HttpClient client, string folder, string url)
    {
        var fileName = Path.GetFileName(url);
        string filePath = Path.Combine(folder, fileName);
        try
        {
            using var stream = await client.GetStreamAsync(url);
            using var file = File.Create(filePath);
            // create a new file to write to
            await stream.CopyToAsync(file); // copy that stream to the file stream
            await file.FlushAsync(); // flush back to disk before disposing
            return filePath;
        }
        catch { }
        return string.Empty;
    }
}
