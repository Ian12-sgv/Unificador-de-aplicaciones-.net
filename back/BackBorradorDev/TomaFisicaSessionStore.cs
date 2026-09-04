using System.Collections.Concurrent;

public sealed class TomaFisicaSessionStore
{
    public const int DurationHours = 12;

    private readonly ConcurrentDictionary<string, Session> sessions =
        new(StringComparer.Ordinal);

    public string Create(string userCode)
    {
        CleanExpired();
        while (true)
        {
            var token = Guid.NewGuid().ToString("N");
            if (sessions.TryAdd(token, new Session(userCode, DateTime.UtcNow.AddHours(DurationHours))))
                return token;
        }
    }

    public string? GetUser(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !sessions.TryGetValue(token, out var session))
            return null;

        if (session.ExpiresUtc > DateTime.UtcNow)
            return session.UserCode;

        sessions.TryRemove(token, out _);
        return null;
    }

    public void Close(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
            sessions.TryRemove(token, out _);
    }

    private void CleanExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var item in sessions)
        {
            if (item.Value.ExpiresUtc <= now)
                sessions.TryRemove(item.Key, out _);
        }
    }

    private sealed record Session(string UserCode, DateTime ExpiresUtc);
}
