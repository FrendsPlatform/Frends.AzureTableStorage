using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Frends.AzureTableStorage.ListTables.Definitions;
using Frends.AzureTableStorage.ListTables.Helpers;

namespace Frends.AzureTableStorage.ListTables;

/// <summary>
/// Task Class for AzureTableStorage operations.
/// </summary>
public static class AzureTableStorage
{
    /// <summary>
    /// List the tables in Azure Table Storage
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-AzureTableStorage-ListTables)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, int Count, List&lt;object&gt; Tables { string Name, string Uri }, object Error { string Message, object AdditionalInfo } }</returns>
    public static async Task<Result> ListTables(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, connection, options);

            var serviceClient = ConnectionHandler.GetTableServiceClient(connection, cancellationToken);

            string filter = null;
            if (!string.IsNullOrWhiteSpace(input.TableNamePrefix))
            {
                var prefix = input.TableNamePrefix.Replace("'", "''");
                filter = $"TableName ge '{prefix}' and TableName lt '{prefix}{{'";
            }

            var tables = new List<TableInfo>();

            await foreach (var table in serviceClient.QueryAsync(filter, cancellationToken: cancellationToken))
            {
                tables.Add(new TableInfo
                {
                    Name = table.Name,
                    Uri = serviceClient.GetTableClient(table.Name).Uri.ToString(),
                });

                if (input.MaxResults > 0 && tables.Count >= input.MaxResults)
                    break;
            }

            return new Result
            {
                Success = true,
                Tables = tables,
                Count = tables.Count,
                Error = null,
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }
}
