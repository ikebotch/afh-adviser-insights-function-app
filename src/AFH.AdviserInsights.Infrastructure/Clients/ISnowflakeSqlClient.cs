namespace AFH.AdviserInsights.Infrastructure.Clients;

public interface ISnowflakeSqlClient
{
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        string statement,
        CancellationToken cancellationToken);
}
