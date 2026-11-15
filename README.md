# WMP X — Windows Media Player Next

WMP X es una aplicación de escritorio nativa para Windows, creada con C# y WinUI 3 (Windows App SDK). No es una aplicación web.

## Requisitos

- Windows 10 versión 1809 (build 17763) o posterior.
- .NET 8 SDK.
- Visual Studio 2022 con la carga de trabajo **Desarrollo de aplicaciones de Windows** (recomendado).

## Ejecutar desde Visual Studio

1. Abre `Wmpx.csproj` en Visual Studio 2022.
2. Deja que NuGet restaure `Microsoft.WindowsAppSDK`.
3. Selecciona la plataforma `x64` y pulsa **F5**.
4. En la aplicación, pulsa **Abrir archivos de audio**, selecciona archivos locales y usa la lista y los controles para reproducirlos.

## Ejecutar desde PowerShell

Desde la carpeta del repositorio, en Windows con .NET 8 instalado:

```powershell
dotnet restore .\Wmpx.csproj
dotnet run --project .\Wmpx.csproj
```

La aplicación usa el reproductor multimedia de Windows para reproducir los archivos seleccionados. Por ahora admite audio local (MP3, WAV, M4A, WMA, AAC y FLAC); otras secciones de la interfaz son una base para futuras versiones.
