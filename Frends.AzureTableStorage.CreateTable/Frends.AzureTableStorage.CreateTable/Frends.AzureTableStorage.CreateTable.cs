using Frends.AzureTableStorage.CreateTable.Definitions;
using Frends.AzureTableStorage.CreateTable.Helpers;
using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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

            var response = await serviceClient.CreateTableIfNotExistsAsync(input.TableName, cancellationToken);
            var rawResponse = response?.GetRawResponse();
            var status = rawResponse?.Status ?? 0;

            bool tableCreated;
            if (status == 201 || status == 204)
            {
                tableCreated = true;
            }
            else if (status == 409)
            {
                var errorCode = GetODataErrorCode(rawResponse);

                if (errorCode != "TableAlreadyExists")
                    throw new Exception($"Failed to create table '{input.TableName}'. Status: {status} ({rawResponse?.ReasonPhrase}). Error code: '{errorCode}'.");

                if (options.FailIfTableExists)
                    throw new Exception($"Table '{input.TableName}' already exists.");

                tableCreated = false;
            }
            else
            {
                throw new Exception($"Failed to create table '{input.TableName}'. Status: {status} ({rawResponse?.ReasonPhrase}).");
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