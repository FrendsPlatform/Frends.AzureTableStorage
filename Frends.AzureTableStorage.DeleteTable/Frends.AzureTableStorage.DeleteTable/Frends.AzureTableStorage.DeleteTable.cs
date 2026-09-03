using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Frends.AzureTableStorage.DeleteTable.Definitions;
using Frends.AzureTableStorage.DeleteTable.Helpers;

namespace Frends.AzureTableStorage.DeleteTable;

/// <summary>
/// Task class for Azure Table Storage operations.
/// </summary>
public static class AzureTableStorage
{
    /// <summary>
    /// Deletes a table from Azure Table Storage.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-AzureTableStorage-DeleteTable)
    /// </summary>
    /// <param name="input">Table parameters including the table name.</param>
    /// <param name="connection">Azure Storage authentication parameters.</param>
    /// <param name="options">Optional behavior controls including error handling.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string TableName, bool Deleted, string TableUri, object Error { string Message, string AdditionalInfo } }</returns>
    public static async Task<Result> DeleteTable(
    [PropertyTab] Input input,
    [PropertyTab] Connection connection,
    [PropertyTab] Options options,
    CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, connection, options);

            var serviceClient = ConnectionHandler.GetTableServiceClient(connection, cancellationToken);

            var tableClient = serviceClient.GetTableClient(input.TableName);
            var tableUri = tableClient?.Uri?.ToString() ?? string.Empty;

            var response = await serviceClient.DeleteTableAsync(input.TableName, cancellationToken);
            var status = response?.Status ?? 0;

            bool tableDeleted;
            if (status == 204)
            {
                tableDeleted = true;
            }
            else if (status == 404)
            {
                var errorCode = GetODataErrorCode(response);

                if (errorCode != "ResourceNotFound")
                    throw new Exception($"Failed to delete table '{input.TableName}'. Status: {status} ({response?.ReasonPhrase}). Error code: '{errorCode}'.");

                if (options.FailIfTableNotExists)
                    throw new Exception($"Table '{input.TableName}' does not exist.");

                tableDeleted = false;
            }
            else
            {
                throw new Exception($"Failed to delete table '{input.TableName}'. Status: {status} ({response?.ReasonPhrase}).");
            }

            return new Result
            {
                Success = true,
                TableName = input.TableName,
                Deleted = tableDeleted,
                TableUri = tableUri,
                Error = null,
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }

    /// <summary>
    /// Extracts the "odata.error.code" value from a raw error response, if present.
    /// </summary>
    private static string GetODataErrorCode(Azure.Response rawResponse)
    {
        try
        {
            var content = rawResponse?.Content;
            if (content == null)
                return null;

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("odata.error", out var odataError) &&
                odataError.TryGetProperty("code", out var code))
            {
                return code.GetString();
            }
        }
        catch
        {
            // Ignore parsing failures - fall back to null (treated as unknown/unexpected error).
        }

        return null;
    }
}