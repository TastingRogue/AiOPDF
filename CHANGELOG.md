# Changelog

## 1.0.0-rc.3

- Instaladores autónomos Windows x64, DMG macOS Apple Silicon y DEB Ubuntu amd64.
- Pruebas de instalación, motor/OCR instalado, arranque de interfaz, reinstalación y retirada en runners nativos.
- Instalador Windows bilingüe, rutas de publicación limpias, dependencias Linux y orden de versiones Debian corregidos.
- Bundle macOS con ensamblados integrados, verificación de firma y acceso directo a Applications.
- Paquetes y evidencia SHA-256 conservados por el workflow de instaladores.

La firma de Windows, notarización macOS y aceptación humana ampliada siguen pendientes.


## 1.0.0-rc.2

- Selector de español e inglés con cambio inmediato, sin perder ajustes, y preferencia local persistente.
- Catálogos JSON modulares con fallback y validación de placeholders; admite idiomas adicionales externos.
- Búsqueda, textos dinámicos, accesibilidad, selectores nativos e informe TXT traducidos.
- Cabecera adaptable y regresiones de las 25 herramientas en ambos idiomas.
- Los valores internos, nombres de archivo y fragmentos de documentos se conservan.

Continúan pendientes los requisitos de aceptación nativa y firma de la versión estable.

## 1.0.0-rc.1

- Sesiones de revisión privadas con bloqueo de actividad y recuperación de temporales tras cierres inesperados.
- Liberación de imágenes sustituidas; detalles de comparación cargados al abrirlos.
- Guardado asíncrono, cancelable, con copia provisional y conservación de destinos existentes.
- Informe y subrayado comparten tokens, coordenadas y cambios.
- Límites de entrada y renderizado; pruebas de cierre forzado y PDF de 500 páginas.
- Licencia MIT, VERSION único, dependencias bloqueadas, CI por plataforma y manifiestos SHA-256.
- Iconsets Mac @2x, ruta de notarización y script opcional de firma Windows.

Esta es una candidata. Faltan aceptación nativa, firma y pruebas de compatibilidad ampliadas antes de anunciar 1.0 estable.

