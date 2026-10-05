using System;
using System.Collections.Generic;
using System.Linq;
using Azure;
using Azure.Data.Tables;
using Frends.AzureTableStorage.InsertEntities.Definitions;

namespace Frends.AzureTableStorage.InsertEntities.Helpers;

internal static class FailedItemBuilder
{
    internal static List<FailedItem> Build(TableEntity[] unit, Exception ex)
    {
        var reason = Describe(ex);

        var entityLevel = ex is RequestFailedException { Status: 400 or 409 };

        var culpritIndex = -1;

        if (entityLevel)
        {
            if (ex is TableTransactionFailedException transaction
                && transaction.FailedTransactionActionIndex is int idx
                && idx >= 0
                && idx < unit.Length)
            {
                culpritIndex = idx;
            }
            else if (unit.Length == 1)
            {
                culpritIndex = 0;
            }
        }

        var culprit = culpritIndex >= 0 ? unit[culpritIndex] : null;

        return
        [
            .. unit.Select((entity, index) => new FailedItem
            {
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey,
                IsCause = index == culpritIndex,
                Reason = culprit == null || index == culpritIndex
                    ? reason
                    : $"Not written: the transaction was not applied because " +
                      $"PartitionKey '{culprit.PartitionKey}', RowKey '{culprit.RowKey}' failed ({reason})",
            })
        ];
    }

    internal static string Describe(Exception ex)
    {
        if (ex is RequestFailedException requestFailed)
        {
            var message = GetFirstLine(requestFailed.Message);

            return $"Status {requestFailed.Status} {requestFailed.ErrorCode}: {message}";
        }

        return GetFirstLine(ex.Message);
    }

    private static string GetFirstLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        return message
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim() ?? string.Empty;
    }
}
