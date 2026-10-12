using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Frends.AzureTableStorage.InsertEntities.Helpers;

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

            if (!item.TryGetValue("PartitionKey", out var pk) || pk is not string { Length: > 0 } ||
                !item.TryGetValue("RowKey", out var rk) || rk is not string { Length: > 0 })
                throw new ArgumentException($"Entity at index {i} must have non-empty string PartitionKey and RowKey.");

            if (item.Values.Any(v => v is JContainer))
                throw new ArgumentException($"Entity at index {i} contains nested objects or arrays, which Table Storage does not support.");

            foreach (var key in item.Where(kv => kv.Value == null).Select(kv => kv.Key).ToList())
                item.Remove(key);

            result.Add(new TableEntity(item));
        }

        return result;
    }
}
