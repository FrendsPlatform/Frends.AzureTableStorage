using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.DeleteEntities.Definitions;
using Frends.AzureTableStorage.DeleteEntities.Helpers;

namespace Frends.AzureTableStorage.DeleteEntities;

/// <summary>
/// Task Class for AzureTableStorage operations.
/// </summary>
public static class AzureTableStorage
{
    /// <summary>
    /// Deletes entities from Azure Table Storage
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-AzureTableStorage-DeleteEntities)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, List&lt;EntityKey&gt; SucceededItems, object Error { string Message, Exception AdditionalInfo, List&lt;FailedItem&gt; FailedItems } }</returns>
    public static async Task<Result> DeleteEntities(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        var succeeded = new List<EntityKey>();
        var failed = new List<FailedItem>();
        var total = 0;

        try
        {
            ValidationHandler.Run(input, connection, options);

            var tableClient = ConnectionHandler.GetTableServiceClient(connection, cancellationToken)
                .GetTableClient(input.TableName);

            var entities = EntityParser.Parse(input.Entities);
            total = entities.Count;

            IEnumerable<TableEntity[]> units = entities
                .GroupBy(e => e.PartitionKey)
                .SelectMany(g => g.Chunk(options.BatchSize));

            foreach (var unit in units)
            {
                try
                {
                    await DeleteUnitAsync(tableClient, unit, cancellationToken);
                    succeeded.AddRange(unit.Select(e => new EntityKey
                    {
                        PartitionKey = e.PartitionKey,
                        RowKey = e.RowKey,
                    }));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed.AddRange(FailedItemBuilder.Build(unit, ex));

                    if (!options.ContinueOnFailure)
                        throw;
                }
            }

            return new Result
            {
                Success = true,
                SucceededItems = succeeded,
                Error = failed.Count == 0
                    ? null
                    : new Error
                    {
                        Message = BuildFailedMessage(options, failed.Count, total),
                        FailedItems = failed,
                    },
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options, succeeded: succeeded, failed: failed, total: total);
        }
    }

    private static async Task DeleteUnitAsync(
        TableClient client, TableEntity[] unit, CancellationToken cancellationToken)
    {
        await client.SubmitTransactionAsync(
            unit.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e, ETagFor(e))).ToList(),
            cancellationToken);
    }

    private static ETag ETagFor(TableEntity entity) =>
        entity.ETag == default ? ETag.All : entity.ETag;

    private static string BuildFailedMessage(Options options, int failedCount, int total)
    {
        var message = $"{failedCount} of {total} entities failed. See Error.FailedItems.";
        return string.IsNullOrEmpty(options.ErrorMessageOnFailure)
            ? message
            : $"{options.ErrorMessageOnFailure}: {message}";
    }
}