using Microsoft.SemanticKernel.Text;

namespace server.Services;
public class TextUtilityService
{
    /// <summary>
    /// Breaks text into chunks of a given size.
    /// </summary>
    /// <param name="content">The text to break into chunks.</param>
    /// <param name="chunk_size">The size of each chunk.</param>
    /// <returns>A list of chunks.</returns>
    public List<string> ChunkText(string content, int chunk_size)
    {
        var lines = TextChunker.SplitPlainTextLines(content, chunk_size / 2);
        // return paragraphs
        return TextChunker.SplitPlainTextParagraphs(lines, chunk_size);
    }
}