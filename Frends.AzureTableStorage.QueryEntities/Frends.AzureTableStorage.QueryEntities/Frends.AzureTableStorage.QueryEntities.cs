using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Frends.AzureTableStorage.QueryEntities.Definitions;
using Frends.AzureTableStorage.QueryEntities.Helpers;
using Newtonsoft.Json.Linq;

namespace Frends.AzureTableStorage.QueryEntities;

/// <summary>
/// Task Class for AzureTableStorage operations.
/// </summary>
public static class AzureTableStorage
{
    /// <summary>
    /// Query a page of entities in Azure Table Storage.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-AzureTableStorage-QueryEntities)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, List&lt;JObject&gt; Entities, int Count, string ContinuationToken, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static async Task<Result> QueryEntities(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, connection);
            ValidateQuery(input);
            cancellationToken.ThrowIfCancellationRequested();

            var tableClient = ConnectionHandler.GetTableServiceClient(connection, cancellationToken)
                .GetTableClient(input.TableName);

            return await QueryPageAsync(tableClient, input, cancellationToken);
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }

    internal static async Task<Result> QueryPageAsync(
        TableClient tableClient, Input input, CancellationToken cancellationToken)
    {
        var select = input.Select is { Length: > 0 }
            ? input.Select.Concat(["PartitionKey", "RowKey"]).Distinct(StringComparer.Ordinal).ToArray()
            : null;
        var query = tableClient.QueryAsync<TableEntity>(
            filter: string.IsNullOrEmpty(input.Filter) ? null : input.Filter,
            maxPerPage: input.Top,
            select: select,
            cancellationToken: cancellationToken);

        await foreach (var page in query.AsPages(
                               continuationToken: string.IsNullOrEmpty(input.ContinuationToken)
                                   ? null
                                   : input.ContinuationToken)
                           .WithCancellation(cancellationToken))
        {
            var entities = new List<JObject>(page.Values.Count);
            foreach (var entity in page.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                entities.Add(EntityMapper.ToJObject(entity));
            }

            return new Result
            {
                Success = true,
                Entities = entities,
                Count = entities.Count,
                ContinuationToken = page.ContinuationToken,
                Error = null,
            };
        }

        return new Result
        {
            Success = true,
            Entities = [],
            Count = 0,
            ContinuationToken = null,
            Error = null,
        };
    }

    private static void ValidateQuery(Input input)
    {
        if (input.Filter is { Length: > 0 } && string.IsNullOrWhiteSpace(input.Filter))
            throw new ValidationException("Filter must contain an OData expression or be empty.");

        if (input.Select?.Any(string.IsNullOrWhiteSpace) == true)
            throw new ValidationException("Select must contain non-empty property names.");

        if (input.ContinuationToken is { Length: > 0 } && string.IsNullOrWhiteSpace(input.ContinuationToken))
            throw new ValidationException("ContinuationToken must be a token or be empty.");
    }
}
