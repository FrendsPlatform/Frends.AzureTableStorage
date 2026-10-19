using System;
using System.Collections.Generic;
using System.Linq;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace Frends.AzureTableStorage.DeleteEntities.Helpers;

internal static class EntityParser
{
    internal static List<TableEntity> Parse(string json)
    {
        var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json)
            ?? throw new ArgumentException("Entities must be a JSON array.");

        var result = new List<TableEntity>();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item is null) throw new ArgumentException($"Entity at index {i} must be a JSON object, not null.");

            if (!item.TryGetValue("PartitionKey", out var pk) || pk is not string { Length: > 0 } ||
                !item.TryGetValue("RowKey", out var rk) || rk is not string { Length: > 0 })
                throw new ArgumentException($"Entity at index {i} must have non-empty string PartitionKey and RowKey.");

            string etag = null;
            if (item.TryGetValue("odata.etag", out var rawEtag))
            {
                if (rawEtag is not null and not string)
                    throw new ArgumentException($"Entity at index {i}: 'odata.etag' must be a string.");

                etag = rawEtag as string;
            }

            var unexpected = item.Keys.Except(["PartitionKey", "RowKey", "odata.etag"]).ToList();
            if (unexpected.Count > 0)
            {
                throw new ArgumentException(
                    $"Entity at index {i} contains unsupported properties ({string.Join(", ", unexpected)}). " +
                    "Only PartitionKey, RowKey and an optional odata.etag are allowed when deleting.");
            }

            var entity = new TableEntity((string)pk, (string)rk);
            if (!string.IsNullOrWhiteSpace(etag))
                entity.ETag = new ETag(etag);

            result.Add(entity);
        }

        return result;
    }
}