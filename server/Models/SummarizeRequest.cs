namespace server.Models;
public record SummarizeRequest(string prompt, string content, int chunk_size, int max_tokens, double temperature);

