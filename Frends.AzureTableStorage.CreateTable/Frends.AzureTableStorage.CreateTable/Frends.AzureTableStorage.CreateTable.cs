using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Frends.AzureTableStorage.CreateTable.Definitions;
using Frends.AzureTableStorage.CreateTable.Helpers;

namespace Frends.AzureTableStorage.CreateTable;

/// <summary>
/// Task class for Azure Table Storage operations.
/// </summary>
public static class AzureTableStorage
{
    /// <summary>
    /// Creates a table in Azure Table Storage.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-AzureTableStorage-CreateTable)
    /// </summary>
    /// <param name="input">Table parameters including the table name.</param>
    /// <param name="connection">Azure Storage authentication parameters.</param>
    /// <param name="options">Optional behavior controls including error handling.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string TableName, bool Created, string TableUri, object Error { string Message, string AdditionalInfo } }</returns>
    public static async Task<Result> CreateTable(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, connection, options);

            var serviceClient = ConnectionHandler.GetTableServiceClient(connection, cancellationToken);

            bool tableCreated = false;

            var response = await serviceClient.CreateTableIfNotExistsAsync(input.TableName, cancellationToken);

            bool tableAlreadyExisted = response?.GetRawResponse()?.Status == 409;
            tableCreated = !tableAlreadyExisted;

            if (tableAlreadyExisted && options.FailIfTableExists)
            {
                throw new Exception($"Table '{input.TableName}' already exists.");
            }

            var tableClient = serviceClient.GetTableClient(input.TableName);
            var tableUri = tableClient?.Uri?.ToString() ?? string.Empty;

            return new Result
            {
                Success = true,
                TableName = input.TableName,
                Created = tableCreated,
                TableUri = tableUri,
                Error = null,
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }
}
