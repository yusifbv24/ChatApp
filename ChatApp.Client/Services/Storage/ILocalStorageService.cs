namespace ChatApp.Client.Services.Storage
{
    public interface ILocalStorageService
    {
        /// <summary>
        /// Stores an item in localStorage with the specified key
        /// </summary>
        Task SetItemAsync<T>(string key, T value);

        /// <summary>
        /// Retrieves an item from localStorage by key
        /// Returns default(T) if the key doesn't exist
        /// </summary>
        Task<T?> GetItemAsync<T>(string key);

        /// <summary>
        /// Removes an item from localStorage
        /// </summary>
        Task RemoveItemAsync(string key);

        /// <summary>
        /// Checks if a key exists in localStorage
        /// </summary>
        Task<bool> ContainKeyAsync(string key);

        /// <summary>
        /// Clears all items from localStorage
        /// Use with caution! This clears EVERYTHING, not just your app's data.
        /// </summary>
        Task ClearAsync();
    }
}