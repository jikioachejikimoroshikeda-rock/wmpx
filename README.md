# WMP X — Windows Media Player Next

Reproductor de escritorio nativo para Windows con audio local, vídeo mediante LibVLC y un visor de imágenes. Diseñado con WinUI 3.

## Ejecutar

Requiere Windows 10 versión 1809 o posterior, .NET 8 SDK y Visual Studio 2022 con **Desarrollo de aplicaciones de Windows**. Abre `Wmpx.csproj`, restaura paquetes NuGet y ejecuta con F5; también puedes ejecutar `dotnet restore .\Wmpx.csproj` y `dotnet run --project .\Wmpx.csproj` desde PowerShell.

## Funciones multimedia

- Audio: controles de reproducción, volumen, mute y cola.
- Vídeo: LibVLC, subtítulos externos, pantalla completa, controles de progreso y volumen.
- Imágenes: abre varios archivos, navega anterior/siguiente y ajusta el zoom.
- Añadir carpeta: importa archivos de audio en modo música o vídeos en modo vídeo desde una carpeta local.
- Temas personalizables y controles visuales de estilo Windows.

El soporte de formatos depende de los códecs y del runtime disponible; no se garantiza que todos los archivos multimedia sean compatibles. Para redistribuir, revisa `THIRD-PARTY-NOTICES.md` y las licencias de las dependencias.

## Licencia

El material original está cubierto por `LICENSE`. Las versiones anteriores distribuidas bajo GPL-3.0 conservan los permisos previamente otorgados. Las dependencias de terceros mantienen sus propias licencias.
