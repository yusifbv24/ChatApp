namespace ChatApp.Client.Services.Storage
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;

        public LocalStorageService(Blazored.LocalStorage.ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        /// <summary>
        /// Stores data with automatic JSON serialization
        /// The value is serialized to JSON and stored as a string.
        /// Complex objects, arrays, and primitives all work automatically.
        /// </summary>
        public async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                await _localStorage.SetItemAsync(key, value);
            }
            catch (Exception)
            {
                // If localStorage is full or disabled, fail silently
                // The application will continue to work, but without persistence
                // (user will need to log in again after page refresh)
                // In production, you might want to log this error
            }
        }

        /// <summary>
        /// Retrieves and deserializes data from storage
        /// Returns null if the key doesn't exist or deserialization fails
        /// </summary>
        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                return await _localStorage.GetItemAsync<T>(key);
            }
            catch (Exception)
            {
                // If deserialization fails or storage is disabled, return default
                return default;
            }
        }

        /// <summary>
        /// Removes an item from storage
        /// Safe to call even if the key doesn't exist
        /// </summary>
        public async Task RemoveItemAsync(string key)
        {
            try
            {
                await _localStorage.RemoveItemAsync(key);
            }
            catch (Exception)
            {
                // Fail silently - the item is either already gone or storage is disabled
            }
        }

        /// <summary>
        /// Checks if a key exists without retrieving the value
        /// More efficient than GetItemAsync when you only need to check existence
        /// </summary>
        public async Task<bool> ContainKeyAsync(string key)
        {
            try
            {
                return await _localStorage.ContainKeyAsync(key);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Clears ALL localStorage
        /// WARNING: This affects the entire origin, not just your app!
        /// If other apps on the same domain use localStorage, their data will be cleared too.
        /// Only use this when absolutely necessary (like a "reset all data" feature).
        /// </summary>
        public async Task ClearAsync()
        {
            try
            {
                await _localStorage.ClearAsync();
            }
            catch (Exception)
            {
                // Fail silently
            }
        }
    }
}