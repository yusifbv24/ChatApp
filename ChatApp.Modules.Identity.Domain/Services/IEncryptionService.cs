namespace ChatApp.Modules.Identity.Domain.Services
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive data
    /// Uses AES-256-GCM for encryption
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts a plaintext string
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <returns>Base64-encoded encrypted string, or null if input is null</returns>
        string? Encrypt(string? plainText);

        /// <summary>
        /// Decrypts an encrypted string
        /// </summary>
        /// <param name="encryptedText">Base64-encoded encrypted text</param>
        /// <returns>Decrypted plaintext, or null if input is null</returns>
        string? Decrypt(string? encryptedText);

        /// <summary>
        /// Encrypts a DateTime value
        /// </summary>
        string? Encrypt(DateTime? dateTime);

        /// <summary>
        /// Decrypts to a DateTime value
        /// </summary>
        DateTime? DecryptDateTime(string? encryptedText);
    }
}