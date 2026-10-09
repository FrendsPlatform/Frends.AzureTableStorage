using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Frends.AzureTableStorage.InsertEntities.Definitions;
using Frends.AzureTableStorage.InsertEntities.Helpers;

namespace Frends.AzureTableStorage.InsertEntities;

/// <summary>
/// Task Class for AzureTableStorage operations.
/// </summary>
public static class AzureTableStorage
{
    /// <summary>
    /// Insert the entities into Azure Table Storage
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-AzureTableStorage-InsertEntities)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, List&lt;EntityKey&gt; SucceededItems, object Error { string Message, Exception AdditionalInfo, List&lt;FailedItem&gt; FailedItems } }</returns>
    public static async Task<Result> InsertEntities(
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
                    await WriteUnitAsync(tableClient, unit, options, cancellationToken);
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

    private static async Task WriteUnitAsync(
        TableClient client, TableEntity[] unit, Options options, CancellationToken cancellationToken)
    {
        {
            var actionType = options.InsertMode switch
            {
                InsertMode.UpsertMerge => TableTransactionActionType.UpsertMerge,
                InsertMode.UpsertReplace => TableTransactionActionType.UpsertReplace,
                _ => TableTransactionActionType.Add,
            };

            await client.SubmitTransactionAsync(
                unit.Select(e => new TableTransactionAction(actionType, e)).ToList(),
                cancellationToken);
        }
    }

    private static string BuildFailedMessage(Options options, int failedCount, int total)
    {
        var message = $"{failedCount} of {total} entities failed. See Error.FailedItems.";
        return string.IsNullOrEmpty(options.ErrorMessageOnFailure)
            ? message
            : $"{options.ErrorMessageOnFailure}: {message}";
    }
}
