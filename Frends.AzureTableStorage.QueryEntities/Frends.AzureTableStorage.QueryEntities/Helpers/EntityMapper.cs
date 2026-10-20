using Azure.Data.Tables;
using Newtonsoft.Json.Linq;

namespace Frends.AzureTableStorage.QueryEntities.Helpers;

internal static class EntityMapper
{
    internal static JObject ToJObject(TableEntity entity)
    {
        var result = new JObject();

        foreach (var property in entity)
        {
            result[property.Key] = property.Value == null
                ? JValue.CreateNull()
                : JToken.FromObject(property.Value);
        }

        return result;
    }
}
