
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace server.Services;
public class PdfExtractService : ExtractTextBaseService<PDFFileType>
{
    private const string Paragraph_Marker = "--xx--";

    public PdfExtractService(FileUtilityService fileService) : base(fileService)
    {

    }

    override public async Task<string> ExtractTextFromFileAsync(string filePath)
    {
        var sb = new StringBuilder();
        using PdfDocument document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            var pageText = ContentOrderTextExtractor.GetText(page, true);
            sb.Append(pageText);
        }
        await Task.Delay(0);

        var result = sb.ToString();

        // Try to do some cleanup
        // Keep the paragraphs, but delete all the newlines in between
        result.Replace("\n\n", Paragraph_Marker);
        result.Replace("\n", " ");
        result.Replace(Paragraph_Marker, "\n\n");
        result = Regex.Replace(result, @"\s+", " ");

        return result;
    }
}

