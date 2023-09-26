
namespace server.Services;
public class TextFileExtractService : ExtractTextBaseService<TextFileType>
{
    public TextFileExtractService(FileUtilityService fileService) : base(fileService)
    {

    }
    override public async Task<string> ExtractTextFromFileAsync(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }
}
