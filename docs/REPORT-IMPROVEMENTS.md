# Mejoras derivadas del estudio práctico — AiOPDF 0.6.0

13 de octubre de 2026. Implementación nativa en C#/Avalonia; procesamiento local sin HTTP ni localhost. Referencia: [estudio de iLovePDF](ILOVEPDF-HANDS-ON.md).

## Implementado

| Área | Comportamiento nuevo | Verificación |
|---|---|---|
| Procesar/revisar/guardar | La acción de cada herramienta prepara una copia temporal. Un panel muestra archivos, tamaño y vista previa de PDF, imágenes o texto. Guardar copia elige destino y no sobrescribe. Office se revisa en su aplicación tras guardar. | Preparación y copia de las 24 herramientas que generan archivos; revisión de contraseña protegida, salida de carpeta y rechazo de sobrescritura. |
| Continuación | Usar este PDF en otra herramienta carga el resultado seleccionado; el menú permite elegir la próxima tarea. | Un PDF de dos páginas generado por división pasa a la siguiente tarea; no se usa el original de seis páginas. |
| Temporales | Las copias de revisión se mantienen durante la sesión para permitir continuación. Se borran al cerrar normalmente; los directorios creados por esta ventana son los únicos elegibles. | Cierre elimina la carpeta temporal usada como entrada. Un cierre forzado o fallo del sistema puede dejar temporales. |
| Dividir | Por página, por rango separado por coma o cada N páginas. Resumen de cantidad y grupos antes de ejecutar. | 1–2 y 3–6 producen dos PDFs de dos/cuatro páginas, con texto y orden conservados; N=2 produce tres documentos. Un rango inválido no deja carpeta parcial. |
| Comparar | Visores original/actualizado, zoom efectivo dentro de cada visor, navegación por página y siguiente/anterior cambio. Fragmentos eliminados/añadidos resaltados en tarjetas. Exportación opcional TXT/JSON. | Cambios de importe, espacios, páginas añadidas/eliminadas, falta de texto y cancelación; selección de cambio y zoom desde controles de la interfaz. |
| Recortar | Dibujo de región sobre página, elección de página de referencia, alcance todas/actual/indicadas, campos sincronizados y deshacer. El margen uniforme es un modo alternativo explícito. | Arrastre de puntero modifica valores usados por el motor. CropBox y giros 0/90/180/270 conservan proporciones de la región visible. |
| Censurar | Dibujo de región, alcance y deshacer; revisión del resultado generado antes de guardar. | Rasterización y región negra existentes conservadas; página original sin modificar. |
| Numerar | Posición en nueve zonas y formatos número/Página n/Página n de N. N es la cantidad de páginas del documento. | Página 3 desde 7 recibe «Página 7» arriba; no se modifica el texto de numeración en página 1. |
| Marca de agua | Posición en nueve zonas y opacidad 1–100%. | Motor y flujos completos con marca añadida y texto original conservado. |
| Imágenes a PDF | Papel ajustado/A4/Letter, orientación y margen; centrado sin deformar. El margen/orientación no cambian el modo ajustado. | La salida A4 horizontal mide 841,89×595,276 puntos. |
| Protección | Repetición de contraseña y mostrar/ocultar. Acción habilitada cuando ambas coinciden y tienen al menos ocho caracteres. | Preparación y revisión de PDF cifrado con contraseña sintética. |
| Controles | Selectores con plantilla propia coherente con botones y campos existentes; estados seleccionado, foco y desactivado. | Popup de opciones abierto y flujos reales con valores diferentes. |

## Validación

- Compilación sin errores ni advertencias.
- Pruebas generales del motor PDF y 16 casos de comparación de contenido.
- Suite de interfaz: navegación, botones, las 25 herramientas a 640×540, 1280×800, 1920×1080 y 2560×1380; 50 ejecuciones de guardado con las muestras A/B.
- Suite específica: 121 comprobaciones de las mejoras, incluyendo preparación, guardado, no sobrescritura, continuación, zoom, arrastre, deshacer y limpieza. Evidencia: `artifacts/improvements-0.6.0.log` y `artifacts/improvement-checks`.
- Capturas revisadas: `docs/screenshots/comparison-reader.png`, `visual-crop.png` y `reviewed-split.png`.
- Ejecutable Windows autónomo: arranque comprobado; cero conexiones TCP observadas en el proceso durante la comprobación breve. Esto no certifica todo comportamiento futuro de red.
- Publicación para Windows x64, Linux x64/ARM64 y macOS Intel/ARM64. macOS/Linux son compilaciones cruzadas: faltan pruebas nativas, empaquetado final en cada sistema y firma/notarización de macOS.

Comandos reproducibles: usar `.tools/dotnet/dotnet.exe run --project tests/AiOPDF.Checks -c Release`, agregando `-- --comparison`, `-- --ui` o `-- --improvements` según la suite. Ejecutar secuencialmente: las suites generan las mismas muestras.

## Diferencias del informe que siguen pendientes

- Censura que retire contenido manteniendo seleccionable el resto de la página. El modo actual rasteriza las páginas tratadas y lo explica; no se presenta como equivalente al resultado web.
- Reconstrucción de tablas, columnas, estilos y cajas editables en conversiones Office; los nombres siguen expresando sus límites.
- Compresión avanzada de recursos y optimización opcional de imágenes conservando texto. Se mantiene la compresión sin pérdida y el informe de ahorro medido.
- Resaltado de regiones de texto sobre los dos PDF, búsqueda de cambios, alineación automática de páginas movidas y comparación de apariencia. El visor actual resalta fragmentos en tarjetas; el motor compara texto por página.
- Máscara de múltiples regiones, asas de redimensionado, marcas de agua de imagen/mosaico y orientación EXIF de imágenes.

## Revisión de las skills

Se aplicaron better-writing y better-ui a los flujos modificados. Las acciones ahora nombran la operación y Guardar copia se reserva al destino final; los errores de guardado explican cómo recuperarse. Los controles conservan estados estáticos de foco/selección y no añaden animaciones.

Las plantillas respetan los puntos de integración documentados por [Avalonia para ComboBox](https://github.com/AvaloniaUI/Avalonia/blob/master/src/Avalonia.Themes.Simple/Controls/ComboBox.xaml); la apariencia y el área de dibujo son propias. No se copió la interfaz ni el motor de iLovePDF.

Approve para los flujos y estados comprobados. No verificado: interacción nativa en macOS/Linux, tecnología de asistencia en sistemas reales y calidad general en documentos distintos del corpus de prueba.
