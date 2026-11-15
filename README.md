# WMP X — Windows Media Player Next

WMP X es un reproductor de escritorio nativo creado con C# y WinUI 3 (Windows App SDK). Está diseñado para Windows; no es una aplicación web ni se ofrece soporte para Linux o macOS.

## Requisitos

- Windows 10 versión 1809 (build 17763) o posterior.
- .NET 8 SDK.
- Visual Studio 2022 con la carga de trabajo **Desarrollo de aplicaciones de Windows** (recomendado).

## Ejecutar desde Visual Studio

1. Abre `Wmpx.csproj` en Visual Studio 2022.
2. Deja que NuGet restaure `Microsoft.WindowsAppSDK`.
3. Selecciona la plataforma `x64` y pulsa **F5**.
4. Pulsa **Abrir archivos de audio**, selecciona archivos locales y usa la lista y los controles para reproducirlos.

## Ejecutar desde PowerShell

En Windows con .NET 8 instalado, desde la carpeta del repositorio:

```powershell
dotnet restore .\Wmpx.csproj
dotnet run --project .\Wmpx.csproj
```

La aplicación usa el reproductor multimedia de Windows y actualmente admite audio local (MP3, WAV, M4A, WMA, AAC y FLAC). Algunas secciones de la interfaz son una base para futuras versiones.

## Licencia y permisos

El Material original de WMP X se ofrece bajo los términos de la [licencia propietaria](LICENSE), no bajo una licencia de código abierto para las versiones cubiertas por esos términos. No se concede permiso general para copiar, modificar, distribuir, incorporar ni explotar comercialmente el Material: solicita autorización escrita al titular antes de hacerlo.

**Aviso sobre versiones anteriores:** una versión de este repositorio se publicó previamente con GPL-3.0. Esta licencia nueva no cancela permisos GPL ya concedidos para copias de esa versión; consulta la sección 4 de `LICENSE`. Las dependencias y demás materiales de terceros conservan sus licencias propias.

La licencia personalizada no es asesoría legal. El titular debería confirmar que controla los derechos de cada contribución y consultar asesoría jurídica si necesita una licencia jurídicamente adaptada a su jurisdicción.
