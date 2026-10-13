# Manual de uso

[Español](USER_MANUAL.es.md) · [English](USER_MANUAL.md) · [Repositorio](../README.es.md)

## Instala y elige el idioma

Descarga el instalador Windows x64 de la [release candidata](https://github.com/TastingRogue/AiOPDF/releases/tag/v1.0.0-rc.2), ejecútalo y abre AiOPDF. No necesitas cuenta ni SDK .NET. La candidata no está firmada; la release incluye un manifiesto SHA-256.

Elige **Español** o **English** en la cabecera. La elección se guarda localmente y no traduce el contenido de tus documentos. La aceptación de instalación en macOS/Linux sigue pendiente: consulta el [checklist](RELEASE-CHECKLIST.md).

## Procesa un documento

1. Encuentra una herramienta mediante la búsqueda o el menú de categorías.
2. Selecciona archivos locales o arrástralos al espacio de trabajo.
3. Revisa las miniaturas y ajusta las opciones. Para un PDF cifrado, abre la sección de contraseña.
4. Pulsa la acción de la herramienta, como **Unir**, **Dividir** o **Añadir marca**.
5. Revisa el resultado preparado, pulsa **Guardar copia** y elige un nombre y ubicación nuevos.

Preparar el resultado todavía no lo guarda en tu destino final. **Abrir carpeta del resultado** aparece después de guardar. Los originales se conservan; no se aceptan nombres de salida que ya existan.

Puedes usar el PDF revisado en otra herramienta. Esto utiliza la copia preparada sin reemplazar el original.

## Páginas y vistas previas

Deja el campo vacío para incluir todas las páginas cuando la herramienta lo permita. Usa `1,3,5-8` para seleccionar páginas. Eliminar páginas requiere una selección explícita y conservar al menos una.

Organizar respeta el orden indicado, por ejemplo `3,1,2`, y permite duplicados. Dividir ofrece un PDF por página, por rango o cada N páginas. Los controles de navegación aparecen cuando hay más páginas fuera del grupo visible.

## Compara dos versiones

Añade exactamente dos PDF: primero el original y después la versión actualizada. Usa **Intercambiar versiones** si están al revés y pulsa **Comparar contenido**.

El resultado aparece dentro de la aplicación. Navega por páginas completas o salta entre cambios de texto. El subrayado rosa marca palabras eliminadas y el verde las añadidas. **Guardar informe** es opcional y exporta TXT/JSON.

La comparación es por número de página. No compara imágenes, significado ni alinea páginas movidas. Las páginas sin texto extraíble requieren OCR antes de comparar.

## Elige la herramienta adecuada

| Quieres… | Usa | Ten en cuenta |
| --- | --- | --- |
| Reducir tamaño | Comprimir PDF | La optimización sin pérdida no reduce todos los archivos. |
| Ocultar márgenes | Recortar PDF | El contenido oculto sigue en el documento. |
| Eliminar información de una región | Censurar región | Las páginas seleccionadas se rasterizan; revisa antes de compartir. |
| Buscar texto en escaneos | OCR a PDF | Modelos de alfabeto latino; revisa el texto reconocido. |
| Exportar párrafos editables | Texto a Word | No conserva el diseño original. |
| Leer líneas en una hoja de cálculo | Texto a Excel | No reconstruye tablas ni fórmulas. |
| Presentar páginas | Páginas a diapositivas | Cada diapositiva contiene una imagen, no texto editable. |
| Abrir una copia sin contraseña | Quitar contraseña | Debes conocer la contraseña existente. |

## Cancela y recupera

La cancelación se comprueba entre operaciones; un renderizado nativo en curso puede tardar en devolver el control. Espera el estado de cancelación antes de cerrar. Si cancelas el guardado de una copia revisada, el resultado preparado queda disponible para otro intento.

Los temporales se eliminan al cerrar normalmente. Tras una interrupción, se limpian las sesiones abandonadas en el siguiente inicio, conservando las activas. Las copias exportadas permanecen donde elegiste guardarlas.

## Reporta un problema

Usa los [formularios de issues](https://github.com/TastingRogue/AiOPDF/issues/new/choose). Incluye versión, sistema, herramienta, pasos y resultado esperado. Reproduce con archivos sintéticos; no subas documentos personales ni contraseñas. Para una vulnerabilidad, usa el [reporte privado](../.github/SECURITY.md).

El historial de desarrollo y las validaciones están en el [índice de documentación](README.md).
