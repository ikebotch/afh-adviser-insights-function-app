namespace AFH.AdviserInsights.Infrastructure.Persistence.Snowflake;

public interface ISnowflakeSqlClient
{
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        string statement,
        CancellationToken cancellationToken);
}
