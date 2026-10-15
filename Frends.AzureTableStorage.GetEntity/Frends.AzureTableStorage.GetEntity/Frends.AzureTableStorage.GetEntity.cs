using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.GetEntity.Definitions;
using Frends.AzureTableStorage.GetEntity.Helpers;

namespace Frends.AzureTableStorage.GetEntity;

/// <summary>
/// Task Class for AzureTableStorage operations.
/// </summary>
public static class AzureTableStorage
{
    /// <summary>
    /// Get the entity from Azure Table Storage
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-AzureTableStorage-GetEntity)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, bool Found, JToken Entity, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static async Task<Result> GetEntity(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, connection, options);

            var tableClient = ConnectionHandler.GetTableServiceClient(connection, cancellationToken)
                .GetTableClient(input.TableName);

            try
            {
                var response = await tableClient.GetEntityAsync<TableEntity>(
                    input.PartitionKey, input.RowKey, cancellationToken: cancellationToken);

                return new Result
                {
                    Success = true,
                    Found = true,
                    Entity = EntityMapper.ToJObject(response.Value),
                    Error = null,
                };
            }
            catch (RequestFailedException ex) when (ex.Status == 404 && ex.ErrorCode != "TableNotFound")
            {
                return new Result { Success = true, Found = false, Entity = null, Error = null };
            }
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }
}