namespace server.Models;
public record Completion(string query, string text, object? usage);
