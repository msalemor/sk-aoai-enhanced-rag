namespace server.Models;
public record CompletionResponse(string content, List<Summary> summaries);
