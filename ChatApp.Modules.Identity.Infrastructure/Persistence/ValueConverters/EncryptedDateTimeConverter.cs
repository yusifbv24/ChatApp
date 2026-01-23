using ChatApp.Modules.Identity.Domain.Services;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ChatApp.Modules.Identity.Infrastructure.Persistence.ValueConverters
{
    /// <summary>
    /// EF Core Value Converter for transparent DateTime encryption/decryption
    /// Automatically encrypts when saving to DB, decrypts when reading from DB
    /// </summary>
    public class EncryptedDateTimeConverter : ValueConverter<DateTime?, string?>
    {
        public EncryptedDateTimeConverter(IEncryptionService encryptionService)
            : base(
                v => encryptionService.Encrypt(v),           // To database: Encrypt
                v => encryptionService.DecryptDateTime(v))   // From database: Decrypt
        {
        }
    }
}