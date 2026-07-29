using AFH.AdviserInsights.Application.Abstractions.Audit;
using AFH.AdviserInsights.Infrastructure.Options;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AFH.AdviserInsights.Infrastructure.Audit;

public sealed class TableStorageAdviserInsightsAuditSink : IAdviserInsightsAuditSink
{
    private readonly TableClient tableClient;
    private readonly ILogger<TableStorageAdviserInsightsAuditSink> logger;

    public TableStorageAdviserInsightsAuditSink(
        IOptions<AdviserInsightsOptions> options,
        ILogger<TableStorageAdviserInsightsAuditSink> logger)
        : this(CreateTableClient(options.Value.Audit), logger)
    {
    }

    internal TableStorageAdviserInsightsAuditSink(
        TableClient tableClient,
        ILogger<TableStorageAdviserInsightsAuditSink> logger)
    {
        this.tableClient = tableClient;
        this.logger = logger;
    }

    public async Task WriteAsync(AdviserInsightsAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        try
        {
            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            await tableClient.AddEntityAsync(ToEntity(auditEvent), cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Failed to persist Adviser Insights audit event {EventId}.", auditEvent.EventId);
        }
    }

    internal static TableEntity ToEntity(AdviserInsightsAuditEvent auditEvent)
    {
        var occurred = auditEvent.OccurredUtc.ToUniversalTime();
        var partitionKey = $"{occurred:yyyyMMdd}|AdviserInsights";
        var rowKey = $"{DateTimeOffset.MaxValue.UtcTicks - occurred.UtcTicks:D19}|{auditEvent.EventId}";

        var entity = new TableEntity(partitionKey, rowKey)
        {
            ["EventId"] = auditEvent.EventId,
            ["OccurredUtc"] = occurred,
            ["OperationName"] = auditEvent.OperationName,
            ["Method"] = auditEvent.Method,
            ["Path"] = auditEvent.Path,
            ["Outcome"] = auditEvent.Outcome,
            ["StatusCode"] = auditEvent.StatusCode,
            ["DurationMs"] = auditEvent.DurationMs
        };

        if (!string.IsNullOrWhiteSpace(auditEvent.QueryString))
            entity["QueryString"] = auditEvent.QueryString;
        if (!string.IsNullOrWhiteSpace(auditEvent.CorrelationId))
            entity["CorrelationId"] = auditEvent.CorrelationId;
        if (!string.IsNullOrWhiteSpace(auditEvent.ActorId))
            entity["ActorId"] = auditEvent.ActorId;
        if (!string.IsNullOrWhiteSpace(auditEvent.FailureReason))
            entity["FailureReason"] = auditEvent.FailureReason;

        return entity;
    }

    private static TableClient CreateTableClient(AdviserInsightsAuditOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("AdviserInsights:Audit:ConnectionString is required when the audit provider is TableStorage.");

        if (string.IsNullOrWhiteSpace(options.TableName))
            throw new InvalidOperationException("AdviserInsights:Audit:TableName is required when the audit provider is TableStorage.");

        return new TableClient(options.ConnectionString, options.TableName);
    }
}
