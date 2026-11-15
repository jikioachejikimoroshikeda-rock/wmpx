# WMP X — Windows Media Player Next

WMP X es un reproductor de escritorio nativo para Windows, creado con C# y WinUI 3 (Windows App SDK). Incluye reproducción de audio local y un reproductor de vídeo LibVLC para ampliar la compatibilidad con códecs y contenedores comunes.

## Requisitos y ejecución

- Windows 10 versión 1809 o posterior, .NET 8 SDK y Visual Studio 2022 con **Desarrollo de aplicaciones de Windows**.
- Abre `Wmpx.csproj`, restaura paquetes NuGet, selecciona `x64` y pulsa **F5**.
- O ejecuta `dotnet restore .\Wmpx.csproj` y `dotnet run --project .\Wmpx.csproj` desde PowerShell.
- Ve a **Vídeos → Abrir vídeos** para cargar una cola.

## Controles de vídeo

- Reproducir/pausar y saltar a anterior/siguiente con los controles principales.
- Barra de progreso con posición y duración; puedes arrastrarla para buscar.
- Volumen y silencio compartidos entre el reproductor de audio y vídeo.
- Pantalla completa desde el icono en la superposición del vídeo; el mismo botón permite volver.
- Carga de subtítulos externos `.srt`, `.ass`, `.ssa`, `.vtt` y `.sub` para el vídeo actual.

LibVLC puede reproducir muchos formatos habituales, pero la compatibilidad depende de los códecs, del archivo y del runtime incluido; no se garantiza que todos los archivos sean reproducibles.

## Componentes y licencias

LibVLCSharp y libVLC son componentes de terceros y conservan sus licencias y obligaciones independientes. Revisa `THIRD-PARTY-NOTICES.md` y los avisos incluidos con los paquetes antes de redistribuir. El Material original de WMP X está cubierto por la licencia de `LICENSE`; los permisos GPL previamente concedidos para versiones anteriores no se revocan.
