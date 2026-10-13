# Estudio práctico de iLovePDF y mejoras para AiOPDF

12 de octubre de 2026. Base local: AiOPDF 0.5.1. Estudio mediante navegación e interacción real con la web, no mediante acceso al código del servidor.

Se utilizaron únicamente los PDF sintéticos `AiOPDF-prueba-A.pdf` y `AiOPDF-prueba-B.pdf`, y resultados derivados de ellos. Se cargaron en iLovePDF con autorización del usuario. Cada uno tiene tres páginas; la segunda es horizontal. Los marcadores y cantidades son ficticios. Este estudio no demuestra calidad general en documentos arbitrarios, formularios, firmas o escaneos difíciles.

## Cobertura y evidencia

Se recorrieron 18 herramientas: Unir, Dividir, Comprimir, Números, Comparar, Censurar, Recortar, Organizar, Marca de agua, Word, Excel, PowerPoint, PDF a JPG, Rotar, OCR, Proteger, JPG a PDF y Desbloquear. Protección y Desbloqueo se revisaron a nivel de interfaz; no se certificó una operación de cifrado/descifrado web. Las demás se ejercitaron con las muestras; Comparar incluyó ambos modos y selección de un cambio. No se ensayaron todas las alternativas de cada herramienta ni se adquirió Premium.

Capturas, descargas y documentos de entrada: `artifacts/ilovepdf-study`. Medidas reproducibles: `artifacts/ilovepdf-study/result-inspection.md`. Inspector: `dotnet run --project tests/AiOPDF.Checks -c Release -- --reference-study`.

## Lo que aprendimos al usarlo

| Operación | Interacción y resultado observado | Diferencia o mejora para AiOPDF |
|---|---|---|
| [Unir](https://www.ilovepdf.com/es/unir_pdf) | Cargar A/B, miniaturas de documentos, ordenar arrastrando, acción Unir y descarga posterior. Seis páginas en orden A/B. | Miniaturas de archivos con número de páginas y tamaño; arrastre y accesibilidad por teclado. |
| [Dividir](https://www.ilovepdf.com/es/dividir_pdf) | Rangos personalizados, fijos, extracción de páginas y opción por tamaño Premium. Configuramos 1–2 y 3–6 del PDF unido: dos PDFs de dos y cuatro páginas. | Añadir modos explícitos: por página, por rangos y cada N páginas. Mostrar cuántos archivos se crearán antes de procesar. Nuestro motor actual solo exporta un PDF por página. |
| [Comprimir](https://www.ilovepdf.com/es/comprimir_pdf) | Tres niveles explicados por calidad/compresión; recomendado por defecto. La pantalla informó 65% de ahorro. Las dos descargas suman 30.911 bytes frente a 86.059 de entrada: reducción medida 64,1%, texto conservado. | Nuestro modo sin pérdida sobre los mismos archivos dio 86.059 bytes. Es una brecha real de optimización; investigar recursos duplicados y recomprimir imágenes de forma opcional. No ofrecer tres niveles decorativos sin mecanismos diferentes. |
| [Numerar](https://www.ilovepdf.com/es/insertar_numeros_de_pagina_pdf) | Posición, márgenes, modo simple/doble cara, inicio, alcance, formatos y fuente. Página 3/inicio 7 dio 7 y conservó el resto. | Añadir posición y formato «Página n de N» con vista previa; mantener semántica consecutiva ya corregida. |
| [Comparar](https://www.ilovepdf.com/es/comparar-pdf) | Dos documentos visibles, desplazamiento sincronizado, modos de texto y superposición. A/B produjo 42 cambios en el modo llamado «Texto semántico». Clicar 125→140 lleva al cambio y resalta ambos textos. | Visor paralelo con navegación por cambio, contador y búsqueda. Separar texto/apariencia; no llamar semántico a nuestro diff de palabras. El nombre de su modo no prueba que interprete el significado. |
| [Censurar](https://www.ilovepdf.com/es/censurar-pdf) | Buscar TEST-000A, revisar coincidencias por página, confirmar marcas y aplicar. El PDF conserva tres páginas y texto de informe, pero no el marcador extraíble. | Prioridad: selección visual, revisión de marcas y deshacer antes de aplicar. Para alcanzar esa conservación de texto hace falta censura real del contenido, no solo pintar negro. Nuestra solución rasteriza la página tratada y pierde todo su texto seleccionable. |
| [Recortar](https://www.ilovepdf.com/es/recortar-pdf) | Arrastrar área, restablecer y elegir página actual/todas. Hubo advertencia de área no detectada y éxito simultáneo; el archivo sí confirmó recorte solo de página 1 a 541,37×435,12 pt. El marcador oculto siguió extraíble. | Rectángulo editable sobre página y alcance explícito. Evitar estados de éxito contradictorios. Mantener aviso de que recorte no elimina datos. |
| [Organizar](https://www.ilovepdf.com/es/organizar-pdf) | Miniaturas con zoom, giro y eliminación; restaurar orden. Eliminar página 3 produjo dos páginas y conservó sus dimensiones. | Integrar acciones sobre la miniatura, selección múltiple y deshacer. Mantener alternativa de botones para teclado. |
| [Marca de agua](https://www.ilovepdf.com/es/insertar_imagen_texto_pdf) | Texto/imagen, posición, mosaico, transparencia, rotación, alcance y encima/debajo. «MUESTRA AIOPDF» aparece en las tres páginas y el texto original sigue extraíble. | Posición visual y controles útiles con vista previa; tamaño/color/opacidad antes que opciones avanzadas. Nuestra marca es texto centrado. |
| [Word](https://www.ilovepdf.com/es/pdf_a_word) | Sin OCR/OCR Premium y motor Solid Documents anunciado. DOCX descargado: 131 párrafos, tres tablas, 12 dibujos. | Conversión completa requiere reconstrucción espacial, tablas y estilos. Nuestro «Texto a Word» es exportación limitada. No equipararla por generar un DOCX válido. |
| [Excel](https://www.ilovepdf.com/es/pdf_a_excel) | Sin OCR/OCR Premium, una/varias hojas. Resultado: tres hojas y 216 celdas. | Detectar tablas y columnas antes de prometer conversión de tablas; nuestra salida Página/Texto no equivale. Verificar tipos numéricos y encabezados con un corpus específico. |
| [PowerPoint](https://www.ilovepdf.com/es/pdf_a_powerpoint) | Sin OCR/OCR Premium. Resultado: tres diapositivas y 333 fragmentos de texto editables, cero partes de imagen. | Reconstruir cajas de texto y vectores es distinto de nuestro pase de imágenes. Mantener nombre Páginas a diapositivas hasta implementar edición real. |
| [PDF a JPG](https://www.ilovepdf.com/es/pdf_a_jpg) | «Páginas a JPG» frente a «Extraer imágenes», calidad normal/alta y número de salidas: tres JPG. | Diferenciar render de página de extracción de recursos. Mostrar estimación de cantidad y opciones de calidad comprensibles. |
| [Rotar](https://www.ilovepdf.com/es/rotar_pdf) | Izquierda/derecha con miniatura que cambia y restablecer. Giro derecha: /Rotate 90° en las tres páginas; texto preservado. | Mostrar giro aplicado antes de guardar y ofrecer restablecer. Distinguir giro de archivo completo y de páginas elegidas. |
| [OCR](https://www.ilovepdf.com/es/ocr-pdf) | Selector de hasta tres idiomas; ensayo con español/inglés terminó y produjo texto buscable. Las dimensiones cambiaron ligeramente a 587,37×831,32 pt. | Selección de idiomas compatibles con modelos instalados, revisión de texto y alcance claro. Conservar la geometría local ya corregida. La prueba no evalúa escaneos difíciles porque la muestra tiene texto. |
| [Proteger](https://www.ilovepdf.com/es/proteger-pdf) | Contraseña y repetición; botón desactivado hasta que coincidan. Revisión de interfaz, sin ensayo de cifrado web. | Confirmación y mostrar/ocultar; validación junto al campo en lugar de depender solo del error final. |
| [JPG a PDF](https://www.ilovepdf.com/es/jpg_a_pdf) | Orientación, A4/Letter/ajustado, márgenes, unir o separar. Se cargaron dos JPG derivados de las muestras y se descargó PDF A4. | Tamaño de papel, ajustar sin deformar, márgenes y EXIF. Nuestra página a 96 DPI fija ofrece menos control. |
| [Desbloquear](https://www.ilovepdf.com/es/desbloquear_pdf) | Carga y acción Desbloquear. Se revisó con muestra sin cifrar; no prueba retirada de contraseña. | Detectar e informar «Este PDF no tiene contraseña», y explicar cuándo se necesita contraseña de propietario. |

## Herramientas nuestras sin equivalente independiente observado

Eliminar páginas se cubre mediante Organizar. PDF a PNG comparte el patrón de exportación de página, pero no se probó una salida PNG web. Extraer texto, Metadatos, Rasterizar PDF, Escala de grises y OCR a texto no aparecieron como herramientas independientes en el catálogo observado. No se les atribuye un comportamiento supuesto de iLovePDF: sus criterios permanecen en la auditoría local.

## Prioridades para el rediseño funcional

1. **Documento como área de trabajo.** Página grande, zoom, navegación y selección directamente sobre miniaturas; panel lateral con ajustes de la tarea. En censura/recorte, dibujar y redimensionar la región, ver las marcas y deshacer. Porcentajes pasan a ajustes avanzados.
2. **Comparación para revisar.** Original/actualizado en paralelo, selección de cambio que navega y resalta, siguiente/anterior cambio, búsqueda y exportación separada. Modo visual opcional claramente distinto del textual.
3. **División real por rangos.** Mostrar grupos y número de salidas; separar «un PDF por página» de «un PDF por rango». Los ejemplos 1–2 y 3–6 deben producir dos archivos.
4. **Procesar, revisar y guardar.** Evitar usar Guardar como nombre universal de la operación. Preparar resultado temporal local, mostrar resumen y ofrecer Guardar copia, Abrir y Continuar con otra herramienta. No crear éxito antes de verificar salida; borrar temporales al finalizar/cancelar.
5. **Opciones con efecto real.** Papel/márgenes en imágenes, posición/formato en números, opacidad/posición en marca de agua. Valores por defecto útiles; controles avanzados se revelan cuando se necesitan.
6. **Mejorar motores sin ocultar límites.** Censura que conserve texto fuera de la zona, compresión de recursos/imágenes y conversiones Office con estructura. Cada capacidad requiere fixtures y criterios propios; no se obtiene copiando una pantalla.

## Criterios concretos para la siguiente implementación

- Unir A/B → seis páginas y archivo de salida visible; cada documento se identifica por nombre, tamaño y número de páginas.
- Dividir 1–2 / 3–6 → exactamente dos PDFs de dos y cuatro páginas; no seis archivos.
- Censurar TEST-000A → marcador no extraíble ni recuperable del render; texto no censurado sigue buscable si el modo promete conservarlo. No considerar suficiente una caja negra superpuesta.
- Recortar solo página 1 → cambian sus dimensiones visibles, otras dos permanecen idénticas en geometría; ayuda indica que texto oculto no se elimina.
- Numerar página 3 desde 7 → solo esa página recibe 7, previsualizado en la posición elegida.
- Comparar → seleccionar 125→140 muestra ambos documentos en ese cambio; Guardar informe permanece opcional.
- Comprimir → informar tamaños medidos y compromiso de calidad; no presentar salida mayor como ahorro.
- Imagen a PDF → A4/Letter/ajustado, orientación y márgenes afectan dimensiones comprobables; se conserva proporción y EXIF se respeta.
- Office editable → verificar contenido, tablas/celdas/cajas editables y distribución, además de apertura del archivo.

Este turno produjo investigación, evidencia e inspector reproducible. No se cambió la interfaz ni se implementaron las mejoras propuestas; se conservó el diseño propio y el requisito de procesamiento nativo local, sin servidor localhost.

La exportación de informe de Comparar se intentó con el botón disponible, pero no se recibió una descarga ni un mensaje de éxito. Por tanto, se verificó la revisión interactiva de cambios, no la exportación del informe web.

Actualización del 13 de octubre: las mejoras implementadas en AiOPDF 0.6.0 y las diferencias aún pendientes están en [REPORT-IMPROVEMENTS.md](REPORT-IMPROVEMENTS.md).
