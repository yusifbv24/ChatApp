namespace ChatApp.Modules.Settings.Application.DTOs
{
    public record DisplaySettingsDto(
        string Theme,
        string Language,
        int MessagePageSize
    );
}