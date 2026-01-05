@echo off
echo Cleaning Visual Studio cache and build artifacts...

REM Close Visual Studio first!
echo Please close Visual Studio before continuing.
pause

REM Clean .vs folder
if exist .vs rmdir /s /q .vs
echo .vs folder deleted

REM Clean all bin and obj folders
for /d /r . %%d in (bin,obj) do @if exist "%%d" rd /s /q "%%d"
echo bin/obj folders deleted

REM Clean NuGet cache
dotnet nuget locals all --clear
echo NuGet cache cleared

REM Restore and rebuild
dotnet restore --force
dotnet build

echo Done! Now open Visual Studio.
pause
