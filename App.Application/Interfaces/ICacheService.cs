namespace App.Application.Interfaces
{
    /// <summary>
    /// Provides asynchronous access to cached values without exposing the cache provider.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Gets a cached value, or returns default when the key is not found.
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

        /// <summary>
        /// Adds or replaces a cached value with an optional expiration duration.
        /// When expiration is omitted, the implementation uses its default duration.
        /// </summary>
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiry = null,
            CancellationToken ct = default);

        /// <summary>
        /// Removes the cached value associated with a key.
        /// </summary>
        Task RemoveAsync(string key, CancellationToken ct = default);

        /// <summary>
        /// Removes cached values whose keys start with the specified prefix.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// The cache implementation does not support removing keys by prefix.
        /// </exception>
        Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    }
}
