namespace server.Models;
public record Query(string collection, string query, int maxTokens = 1000, int limit = 3, double minRelevanceScore = 0.77);
