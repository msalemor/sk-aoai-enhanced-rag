namespace server.Models;

/// <summary>
/// Record to work with memories withing the system.
/// Not to be confused with SK MemoryRecord.
/// </summary>
/// <param name="collection"></param>
/// <param name="key"></param>
/// <param name="text"></param>
/// <param name="metadata"></param>
public record Memory(string collection, string key, string text, string? metadata = null);
