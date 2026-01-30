cd C:\Users\ybaghiyev\Desktop\ChatApp\ChatApp.Api

  echo "`nDropping all databases..." -ForegroundColor Yellow

  dotnet ef database drop --force --context IdentityDbContext
  dotnet ef database drop --force --context ChannelsDbContext
  dotnet ef database drop --force --context DirectMessagesDbContext
  dotnet ef database drop --force --context FilesDbContext
  dotnet ef database drop --force --context NotificationsDbContext
  dotnet ef database drop --force --context SearchDbContext
  dotnet ef database drop --force --context SettingsDbContext

  echo "`nApplying migrations..." -ForegroundColor Green

  dotnet ef database update --context IdentityDbContext
  dotnet ef database update --context ChannelsDbContext
  dotnet ef database update --context DirectMessagesDbContext
  dotnet ef database update --context FilesDbContext
  dotnet ef database update --context NotificationsDbContext
  dotnet ef database update --context SearchDbContext
  dotnet ef database update --context SettingsDbContext

  Write-Host "`nDone! Run 'dotnet run' to start the API." -ForegroundColor Green
