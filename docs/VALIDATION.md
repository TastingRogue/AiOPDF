# Validación funcional 0.3.0 — 12 de octubre de 2026

Entorno: Windows, SDK .NET 10.0.401 local. Las 43 comprobaciones del motor PDF pasaron. Además, las 25 herramientas se ejecutaron desde la ventana de Avalonia con los campos que muestra cada pantalla y el mismo método de guardado utilizado por el diálogo real (`SaveToPath`). Se usaron documentos de tres páginas, PDFs cifrados e imágenes PNG/JPG/WebP. No es solo navegación de botones.

## Cobertura de las 25 herramientas

| Herramienta | Resultado comprobado |
|---|---|
| Unir PDF | Seis páginas; abrir y renderizar el resultado |
| Organizar páginas | Orden/duplicados y recuento; texto manual sincronizado con casillas, botones de movimiento y selección vacía |
| Dividir PDF | Tres PDF independientes |
| Eliminar páginas | Dos páginas; rechazo de eliminar todas y rangos fuera del documento |
| Rotar PDF | Rotación seleccionada y conservación de páginas restantes |
| Imágenes a PDF | Tres páginas a partir de PNG/JPG/WebP; renderizado |
| PDF a PNG | Tres imágenes decodificables |
| PDF a JPG | Tres imágenes decodificables |
| Extraer texto | Texto conocido en UTF-8 |
| Marca de agua | Texto añadido conservando texto original |
| Numerar páginas | Número inicial añadido |
| Recortar PDF | CropBox reducido y resultado renderizable |
| Metadatos | Edición de título/autor y eliminación de esos datos |
| Proteger PDF | AES-256; exige contraseña y abre con la correcta |
| Quitar contraseña | PDF cifrado abre sin contraseña tras guardar; contraseña incorrecta rechazada |
| Aplanar PDF | Tres páginas renderizables sin texto original |
| Comprimir PDF | Tres páginas rasterizadas renderizables; no se garantiza reducción de tamaño |
| Escala de grises | Tres páginas rasterizadas renderizables |
| Comparar PDFs | Tres imágenes de diferencias y reporte; documentos iguales dan 0.00% |
| OCR a PDF | Tres páginas buscables y renderizables; texto conocido reconocido |
| OCR a texto | Texto conocido reconocido en salida UTF-8 |
| PDF a Word | DOCX válido según Open XML y contenido conocido |
| Texto a Excel | XLSX válido según Open XML y contenido conocido |
| PDF a diapositivas | PPTX válido según Open XML; tres diapositivas con imágenes |
| Censurar región | Tres páginas renderizables; región negra y ausencia de texto original |

Se comprobaron además cancelación desde el botón de la interfaz, rechazo de sobrescribir originales, conservación byte a byte de la entrada y limpieza de staging tras errores/cancelación.

## Fallos encontrados y corregidos

PDFtoImage cerraba los streams reutilizados después de renderizar la primera página. Se corrigieron las llamadas con `leaveOpen: true` en PdfEngine, OcrOperations y OfficeExports. La prueba de tres páginas evita que una exportación de una sola página vuelva a ocultar este fallo.

La ilustración de éxito permanecía visible al repetir un guardado. Ahora se oculta antes del nuevo trabajo. Los números de páginas y las casillas del organizador ahora comparten selección; quitar todas las casillas no vuelve a incluir todas las páginas accidentalmente.

## Comandos

```powershell
& ./.tools/dotnet/dotnet.exe run --project tests/AiOPDF.Checks -c Release
& ./.tools/dotnet/dotnet.exe run --project tests/AiOPDF.Checks -c Release -- --ui
```

Avalonia.Headless comprobó búsqueda, filtros, foco y renderizado a 1220×880, 860×620 y 640×540 DIP. Las capturas están en screenshots/.

## Límites de esta validación

No se automatizaron los selectores nativos de archivos/carpetas: el destino se entrega al método compartido del flujo de guardado. No se probó lector de pantalla real, 200% de escala del sistema, corpus amplio de PDFs dañados, todos los idiomas OCR ni compatibilidad visual completa con aplicaciones Office. Los límites de formato de las conversiones permanecen explícitos en la interfaz.

Los paquetes de Mac/Linux se compilan desde Windows; su ejecución todavía necesita equipos de esas plataformas. Esta validación no afirma paridad completa con iLovePDF ni prueba exhaustiva de todos los documentos posibles.

## Instalador Windows y verificaciones adicionales
AiOPDF-0.3.0-windows-x64-setup.exe: instalación silenciosa en artifacts/install-validation-0.3, arranque del ejecutable instalado y desinstalación pasaron. Se verificó que no quedó ejecutable ni registro de desinstalación. No se reemplazó ninguna instalación existente. Durante el arranque se observaron cero conexiones TCP y cero endpoints UDP del PID; esto no equivale a auditoría exhaustiva de red.
Se añadieron tres comprobaciones del motor: comparación de documentos diferentes, grises a partir de una imagen coral y cancelación después de empezar a exportar páginas. Total del motor: 43 comprobaciones.


## Rediseño 0.4

Validación completada: 43 comprobaciones del motor, los 25 guardados por la interfaz y pruebas del nuevo diseño con teclado y ratón. Ctrl+F fue corregido para enfocar el campo después de reconstruir el inicio. Se verificaron Enter, Space, Escape, categorías, selección de páginas, control numérico y giro real. Búsqueda y documentos compatibles se conservan al navegar.

Las capturas se generaron en Avalonia Headless a 1220, 860 y 640 px. Los cuadros nativos de archivos y lectores de pantalla físicos no se automatizaron. Las limitaciones de las operaciones y de validación en Mac/Linux siguen aplicando.

Se publicaron los cinco paquetes de 0.4 (Windows x64, macOS x64/ARM64, Linux x64/ARM64). El ejecutable Windows publicado permaneció activo durante la prueba de arranque y el instalador 0.4 compiló correctamente. No se repitió instalación/desinstalación porque ya existe un registro de instalación de AiOPDF; la prueba aislada documentada arriba corresponde a 0.3. Mac y Linux no se ejecutaron en sus sistemas.

## 0.4.1 — iconos y dos documentos de prueba

Se generaron output/pdf/AiOPDF-prueba-A.pdf y AiOPDF-prueba-B.pdf. Cada uno tiene tres páginas: texto y tabla con datos ficticios, página horizontal y página rasterizada para OCR. Los importes y etiquetas difieren para comparar. Se renderizaron las seis páginas y se revisaron las vistas horizontal y escaneada.

Los 25 flujos de guardado de la interfaz pasaron con cada documento (50 ejecuciones). Unir y comparar usan ambos; quitar contraseña usa una copia cifrada de cada uno. Imágenes a PDF utiliza PNG, JPG y WebP derivados de los documentos. Se verifican recuentos, apertura/renderizado de PDF, imágenes legibles, texto y contenido de Office. También pasan las pruebas de errores, cancelación, preservación de originales y limpieza de temporales.

Se encontró y corrigió una deformación de miniaturas: PDFtoImage recibía ancho fijo sin preservar proporción. Ahora WithAspectRatio está activado y se comprueba orientación de las seis vistas previas. Los resultados temporales de los flujos se eliminan; permanecen los dos PDF y sus seis vistas previas.

Esta cobertura usa documentos sintéticos y no garantiza compatibilidad con todos los PDF reales. Persisten las limitaciones de OCR, Office y rasterizado descritas arriba.

También pasaron las 43 comprobaciones del motor tras la corrección. El paquete Windows 0.4.1 inició correctamente. Se publica en artifacts/windows-x64-0.4.1 para no modificar la aplicación anterior abierta.

El instalador Windows 0.4.1 compiló correctamente y tiene SHA-256 adjunto. También se publicaron 0.4.1 para macOS x64/ARM64 y Linux x64/ARM64; falta ejecutarlos en sus sistemas. No se modificó la sesión de la aplicación anterior abierta.

## Auditoría de botones 0.4.2

Detalle en BUTTON-VALIDATION.md. Se verificaron las 25 tarjetas y el regreso desde cada pantalla, categorías, controles numéricos, orden de archivos, eliminación, navegación de ocho páginas, actualización de vista previa, ambos sentidos de orden de páginas y tres giros con PDF reales. Pruebas nativas Windows: seleccionar ambos PDF, guardar el PDF unido, dividir en una carpeta nueva, cancelar diálogos, abrir File Explorer y abrir el selector desde Guardar en el encabezado de una ventana de 860 px.

Se corrigieron dos errores relacionados: actualizar no conservaba selección/orden y un TextChanged diferido podía revertir una selección vacía. Las comprobaciones de botones y los 50 guardados se repitieron con la corrección.

Se repitieron satisfactoriamente la auditoría de botones y los 50 guardados después de ambas correcciones. --native-results confirmó seis páginas en el PDF unido y tres PDF individuales que abren y renderizan. El paquete final Windows inició correctamente; la interacción nativa se comprobó en Windows, no en macOS/Linux.

### 0.4.3 · Panel de preparación y resultado
La revisión automatizada mantiene la navegación, los botones y los 50 flujos de guardado (25 herramientas con cada PDF de prueba). Cada guardado comprueba además que la ubicación mostrada corresponde a la salida real. Revisión visual del estado guardado en docs/screenshots/result-panel.png. Los diálogos nativos conservan los controladores previamente comprobados; esta revisión no equivale a una nueva ejecución nativa en macOS o Linux.


### 0.4.4 · Navegación de vista previa
Anterior y Siguiente se ocultan para documentos de hasta seis páginas. Para documentos mayores, Anterior está desactivado en el primer grupo y Siguiente en el último. Ambos se desactivan durante la carga. Validación --ui-preview aprobada: navegación 1–6 / 7–8 en PDF de ocho páginas, límites y ausencia de controles en PDF corto; regresión de botones y navegación aprobada.


### 0.4.5 · Regreso y versión visible
El botón Volver a herramientas solo aparece por debajo de 1000 px, cuando se oculta el menú lateral. Se comprobó la ausencia del botón en escritorio y el regreso en modo compacto en las 25 herramientas, junto con la navegación de seis páginas y sus límites. El pie muestra la versión real del ensamblado para distinguir ejecutables antiguos.


### 0.4.6 · Revisión responsiva
ResponsiveChecks revisa inicio y las 25 herramientas en 640×540, 1280×800, 1920×1080 y 2560×1380 (100 combinaciones de herramientas). Comprueba límites horizontales de botones/campos, ancho de opciones y crecimiento de imágenes; genera capturas representativas de inicio, Eliminar, Censurar e Imágenes. Revisión visual de capturas grandes y pequeñas. La prueba completa --ui incluye también navegación/botones y 50 flujos con ambos PDFs. Compilación cruzada de macOS y Linux; ejecución nativa pendiente en esos sistemas.


### 0.5.0 · Comparación de contenido y acciones intuitivas
Se sustituyó la diferencia de píxeles por diferencias de palabras, con texto añadido/eliminado y contexto. 16 comprobaciones específicas: igualdad pese a fuente/posición/color, importes distintos, contexto, espacios/saltos de línea, reconstrucción de ambos textos, páginas añadidas/eliminadas, páginas sin texto, memoria acotada, cancelación y exportación. Los 43 controles del motor pasaron. La prueba --ui completa cubre los 50 flujos, 100 combinaciones de tamaño y herramienta y los botones. El botón Comparar contenido inicia la comparación sin selector de destino; Guardar informe aparece después. Seleccionar un archivo preserva el resultado; intercambiar versiones lo invalida. Exportación de comparación: TXT y JSON, sin imágenes de diferencias. Los escaneos quedan pendientes de OCR, sin afirmar igualdad. Revisión visual: docs/screenshots/content-comparison.png. Compilación cruzada macOS/Linux; ejecución nativa pendiente allí.


### Auditoría semántica de 0.5.0
La matriz docs/TOOL-BEHAVIOR-AUDIT.md sustituye la interpretación general de ‘25 herramientas correctas’. --audit registra problemas no cubiertos por los checks de generación/apertura: compresión 20.727→143.520 bytes, numeración seleccionada con inicio ambiguo, censura que extrae solo páginas elegidas y geometría incorrecta después de recortar en cinco operaciones. Evidencia: artifacts/behavior-audit/measurements.md. Los problemas siguen presentes en 0.5.0; esta auditoría no altera el motor.

# Validación de correcciones 0.5.1

Se convirtió la auditoría funcional en comprobaciones que fallan ante aumento de tamaño/pérdida de texto al comprimir, numeración inicial incorrecta, eliminación de páginas no seleccionadas o deformación del recorte. Incluye censura parcial y CropBox desplazada con rotación de 90°. La evidencia actual está en `artifacts/behavior-audit/measurements.md`.

Las exportaciones limitadas se nombran según su resultado: Rasterizar PDF, Texto a Word y Páginas a diapositivas. No equivale a aplanado vectorial de formularios ni reconstrucción editable de Office. La matriz histórica y las capacidades todavía pendientes permanecen en `docs/TOOL-BEHAVIOR-AUDIT.md`.


## AiOPDF 0.6.0 — 13 de octubre
Ver [validación de mejoras del informe](REPORT-IMPROVEMENTS.md). Suite específica: 121 comprobaciones; revisión y guardado de 24 herramientas, comparación separada, continuación, arrastre, zoom y limpieza de temporales. Suite previa de 50 flujos y 100 combinaciones de herramientas/tamaños pasada. Ejecutar suites secuencialmente porque regeneran las mismas muestras.

