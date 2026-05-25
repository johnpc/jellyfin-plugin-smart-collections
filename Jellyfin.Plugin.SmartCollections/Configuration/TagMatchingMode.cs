namespace Jellyfin.Plugin.SmartCollections.Configuration
{
    /// <summary>
    /// Defines the matching mode for multiple tags in a smart collection.
    /// </summary>
    public enum TagMatchingMode
    {
        /// <summary>
        /// Match any tag (OR logic, backward compatible default).
        /// </summary>
        Or = 0,

        /// <summary>
        /// Match all tags (AND logic).
        /// </summary>
        And = 1,
    }
}
