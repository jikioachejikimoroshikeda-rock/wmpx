# WMP X — Windows Media Player Next

WMP X es un reproductor de escritorio nativo para Windows, creado con C# y WinUI 3 (Windows App SDK). Incluye reproducción de audio local con el motor multimedia de Windows y una vista de vídeo basada en LibVLCSharp/libVLC para ampliar la compatibilidad con códecs y contenedores comunes. La compatibilidad real depende del códec, del archivo y de los módulos disponibles; ningún reproductor puede garantizar todos los formatos.

## Requisitos

- Windows 10 versión 1809 (build 17763) o posterior.
- .NET 8 SDK.
- Visual Studio 2022 con la carga de trabajo **Desarrollo de aplicaciones de Windows** (recomendado).
- Acceso a NuGet para restaurar Windows App SDK, LibVLCSharp y el runtime de libVLC.

## Ejecutar

1. Abre `Wmpx.csproj` en Visual Studio 2022 y deja que NuGet restaure los paquetes.
2. Selecciona la plataforma `x64` y pulsa **F5**.
3. Ve a **Vídeos**, pulsa **Abrir vídeos** y elige uno o varios archivos.

También desde PowerShell en Windows:

```powershell
dotnet restore .\Wmpx.csproj
dotnet run --project .\Wmpx.csproj
```

La vista de vídeo usa LibVLC/libVLCSharp y permite cargar una cola de archivos, seleccionar vídeos, reproducir/pausar, saltar entre elementos y buscar en la posición con la barra. El selector incluye contenedores habituales como MP4, MKV, AVI, MOV, WMV, FLV, WebM, MPEG, TS, OGV y 3GP. La extensión no garantiza que un archivo concreto pueda decodificarse.

## Dependencias de terceros

LibVLCSharp y los binarios de libVLC son componentes de terceros y conservan sus propias licencias, avisos y obligaciones de distribución. Consulta los metadatos de los paquetes NuGet y `THIRD-PARTY-NOTICES.md` antes de redistribuir la aplicación; esta licencia de WMP X no reemplaza ni restringe los derechos otorgados por esas licencias de terceros.

## Licencia y permisos

El material original de WMP X se rige por la [licencia propietaria](LICENSE). Una versión anterior se publicó bajo GPL-3.0; esta nueva licencia no revoca los permisos que ya se concedieron para copias de aquella versión. Consulta la sección 4 de `LICENSE` y el texto de las licencias de terceros.
