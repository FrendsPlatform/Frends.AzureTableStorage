using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Frends.AzureTableStorage.DeleteEntities.Definitions;

namespace Frends.AzureTableStorage.DeleteEntities.Helpers;

internal static class FailureSummary
{
    internal static string Build(
        Exception ex,
        int total,
        List<EntityKey> succeeded,
        List<FailedItem> failed)
    {
        var notAttempted = total - succeeded.Count - failed.Count;
        var cause = failed.FirstOrDefault(f => f.IsCause) ?? failed.FirstOrDefault();

        var sb = new StringBuilder(FailedItemBuilder.Describe(ex));

        if (succeeded.Count > 0)
        {
            var succeededItems = string.Join(
                ", ",
                succeeded.Select(e => $"'{e.PartitionKey}/{e.RowKey}'"));

            sb.Append(
                $" | Deleted before the failure: {succeeded.Count} [{succeededItems}].");
        }
        else
        {
            sb.Append(" | Deleted before the failure: 0.");
        }

        sb.Append($" Not deleted: {failed.Count}.");
        sb.Append($" Not attempted: {notAttempted}.");

        if (cause != null)
        {
            sb.Append(
                $" Cause: PartitionKey '{cause.PartitionKey}', " +
                $"RowKey '{cause.RowKey}' - {cause.Reason}");
        }

        return sb.ToString();
    }
}