namespace server.Services;

public class PdfFileType { }
public class TextFileType { }


public interface IExtractText<T>
{
    Task<string> ExtractTextFromUrl(HttpClient client, string url, string folder);
    Task<string> ExtractTextFromFileAsync(string filePath);
}
