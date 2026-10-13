namespace Frends.AzureTableStorage.UpdateEntities.Definitions
{
    /// <summary>
    /// Key of a successfully updated entity.
    /// </summary>
    public class EntityKey
    {
        /// <summary>
        /// PartitionKey of the entity.
        /// </summary>
        /// <example>pk1</example>
        public string PartitionKey { get; set; }

        /// <summary>
        /// RowKey of the entity.
        /// </summary>
        /// <example>1</example>
        public string RowKey { get; set; }
    }
}
