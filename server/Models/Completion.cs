namespace server.Models;
public record Completion(string query, string text, object? usage, List<LearnMore>? learnMore = null, string? deepText = null);

public record LearnMore(string collection, string key, string name);
