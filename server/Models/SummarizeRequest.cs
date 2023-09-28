namespace server.Models;
public record SummarizeRequest(string key, string prompt, string content, int chunk_size, int max_tokens, double temperature);
public record GptSummarizeRequest(string key, string prompt, int chunk_size, int max_tokens, double temperature);

