namespace ChatApp.Blazor.Client.Helpers;

/// <summary>
/// Avatar rəng və görünüş helper-ləri.
/// Bütün komponentlər bu helper-i istifadə edirlər.
/// </summary>
public static class AvatarHelper
{
    /// <summary>
    /// 10 rəng paleti - hər istifadəçi/channel üçün unikal rəng.
    /// </summary>
    private static readonly string[] AvatarColors =
    [
        "#E63946", // Dark Red
        "#2A9D8F", // Dark Teal
        "#2891B5", // Dark Blue
        "#F4845F", // Dark Salmon
        "#6FC0A8", // Dark Mint
        "#E9C46A", // Dark Yellow
        "#9D6EBF", // Dark Purple
        "#5A9FCC", // Dark Sky Blue
        "#E89570", // Dark Peach
        "#7BC4C8"  // Dark Cyan
    ];

    /// <summary>
    /// Guid-dən avatar background rəngi seçir.
    /// Eyni ID həmişə eyni rəngi qaytarır.
    /// </summary>
    /// <param name="id">İstifadəçi, channel və ya conversation ID</param>
    /// <returns>Hex rəng kodu (məs: #E63946)</returns>
    public static string GetAvatarBackgroundColor(Guid id)
    {
        var hash = id.GetHashCode();
        var index = Math.Abs(hash) % AvatarColors.Length;
        return AvatarColors[index];
    }
}