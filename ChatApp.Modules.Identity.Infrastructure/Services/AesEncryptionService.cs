using ChatApp.Modules.Identity.Domain.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace ChatApp.Modules.Identity.Infrastructure.Services
{
    /// <summary>
    /// AES-256-GCM encryption service for encrypting sensitive data
    /// Thread-safe, high-performance implementation
    /// </summary>
    public class AesEncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private const int NonceSize = 12; // 96 bits (recommended for GCM)
        private const int TagSize = 16;   // 128 bits authentication tag (m?lumat?n d?yi?dirildiyini yoxlamaq üçün)

        public AesEncryptionService(IConfiguration configuration)
        {
            var keyString = configuration["EncryptionSettings:Key"]
                ?? throw new InvalidOperationException("Encryption key not configured in appsettings");

            // Convert base64 key to bytes (key must be 32 bytes for AES-256)
            _key = Convert.FromBase64String(keyString);

            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption key must be 32 bytes (256 bits) for AES-256");
        }

        public string? Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                // Generate random nonce (H?r ?m?liyyat üçün unikal)
                var nonce = new byte[NonceSize];
                RandomNumberGenerator.Fill(nonce);

                // Convert plaintext to bytes
                var plainBytes = Encoding.UTF8.GetBytes(plainText);

                // Prepare ciphertext buffer (plaintext + tag)
                var cipherText = new byte[plainBytes.Length];
                var tag = new byte[TagSize];

                // Encrypt using AES-256-GCM
                using var aesGcm = new AesGcm(_key,TagSize);
                aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);

                // Combine: nonce + tag + ciphertext
                var result = new byte[NonceSize + TagSize + cipherText.Length];
                Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
                Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
                Buffer.BlockCopy(cipherText, 0, result, NonceSize + TagSize, cipherText.Length);

                // Return as base64
                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        public string? Decrypt(string? encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                // Decode from base64
                var fullCipher = Convert.FromBase64String(encryptedText);

                // Extract: nonce + tag + ciphertext
                var nonce = new byte[NonceSize];
                var tag = new byte[TagSize];
                var cipherText = new byte[fullCipher.Length - NonceSize - TagSize];

                Buffer.BlockCopy(fullCipher, 0, nonce, 0, NonceSize);
                Buffer.BlockCopy(fullCipher, NonceSize, tag, 0, TagSize);
                Buffer.BlockCopy(fullCipher, NonceSize + TagSize, cipherText, 0, cipherText.Length);

                // Decrypt using AES-256-GCM
                var plainBytes = new byte[cipherText.Length];
                using var aesGcm = new AesGcm(_key, TagSize);
                aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);

                // Convert to string
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Decryption failed. Data may be corrupted or key is incorrect.", ex);
            }
        }

        public string? Encrypt(DateTime? dateTime)
        {
            if (dateTime == null)
                return null;

            // Convert DateTime to ISO 8601 string (sortable, unambiguous)
            var isoString = dateTime.Value.ToString("O"); // "O" = round-trip format
            return Encrypt(isoString);
        }

        public DateTime? DecryptDateTime(string? encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return null;

            var decrypted = Decrypt(encryptedText);
            if (string.IsNullOrEmpty(decrypted))
                return null;

            return DateTime.Parse(decrypted);
        }
    }
}