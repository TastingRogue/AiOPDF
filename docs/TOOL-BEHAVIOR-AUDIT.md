# Auditoría del comportamiento de las 25 herramientas

## Correcciones verificadas en 0.5.1

La matriz siguiente conserva el diagnóstico histórico de 0.5.0. En 0.5.1 se corrigieron los errores reproducibles y se hicieron explícitos los contratos de las exportaciones limitadas:

- Compresión sin rasterizar: comprime streams sin filtros mediante Flate, conserva texto e imágenes y solo entrega la optimización si el archivo final es menor. Si no mejora, entrega una copia idéntica con un mensaje explícito. Una muestra sin comprimir pasó de 25.995 a 18.804 bytes; la muestra ya comprimida permaneció en 20.727 bytes, conservando texto.
- Numeración consecutiva de páginas seleccionadas: página 3, inicio 7, produce 7.
- Censura, grises, rasterizado y OCR a PDF conservan las páginas no seleccionadas. La censura parcial se verifica por píxeles interiores negros, exteriores blancos y ausencia del texto original de la página procesada.
- Rasterizado y OCR usan la región visible (CropBox intersectada con MediaBox), rotación y UserUnit. Las pruebas incluyen recorte desplazado y rotación de 90°. La compresión conserva las cajas originales, sin reconstruir páginas.
- «Aplanar PDF» se presenta como «Rasterizar PDF». No se ha implementado aplanado selectivo de formularios: `PdfDocument.Flatten()` de PDFsharp 6.2.4 únicamente establece ReadOnly en los campos, por lo que no se usa para prometer esa capacidad.
- «Texto a Word» y «Páginas a diapositivas» indican el contenido realmente exportado. Dividir explica que crea un PDF por página; metadatos explica el alcance de limpieza; comparar advierte sobre páginas movidas. La marca de agua larga ajusta su tamaño al ancho de página.

Validación: 44 comprobaciones del motor, 16 de comparación, 50 flujos de herramientas y 100 combinaciones de herramienta/tamaño pasaron. `--audit` ahora incluye aserciones de regresión y conserva los documentos sintéticos como evidencia. Las capacidades avanzadas pendientes de la matriz (estructuras documentales, firmas, EXIF, fidelidad Office, corpus OCR difícil y alineación de páginas) siguen sin certificarse; esta actualización no las declara resueltas.

Fecha: 12 de octubre de 2026. Versión examinada: AiOPDF 0.5.0.

## Conclusión

La aplicación genera archivos válidos en los casos básicos ensayados, pero eso no demuestra que todas las herramientas cumplan las expectativas asociadas a su nombre. Hay errores de geometría, decisiones de alcance confusas y conversiones simplificadas. No debe describirse el conjunto como equivalencia completa con iLovePDF o Acrobat.

Esta auditoría contrasta documentación primaria, código del motor y resultados reproducibles. Las funcionalidades de productos de referencia sirven para definir expectativas; no todas sus opciones son requisitos de AiOPDF. Las propuestas de comportamiento de nuestra aplicación se identifican como tales.

## Matriz de comportamiento

“Básico verificado” se limita a los documentos y propiedades comprobados: no certifica formularios, marcadores, firmas, archivos dañados ni cualquier PDF arbitrario. “Limitado” indica un subconjunto anunciado. “Desajuste” indica un resultado que exige corregir el motor o decidir explícitamente el contrato de uso.

| Herramienta | Comportamiento de referencia / criterio | Estado real en AiOPDF | Veredicto y cobertura pendiente |
|---|---|---|---|
| Unir PDF | Concatenar páginas según el orden elegido [S1]. | Conserva orden, dimensiones y texto básico de las páginas importadas. | Básico verificado. Pendiente: formularios, destinos internos, marcadores, firmas y contraseñas distintas por archivo. |
| Organizar páginas | Reordenar, extraer y duplicar páginas [S1]. | La secuencia `3,1,1` produce esas tres páginas; la vista previa comparte selección con el campo. | Básico verificado. Importación de estructuras documentales avanzadas pendiente. |
| Dividir PDF | Crear archivos de páginas o grupos elegidos [S1]. | Crea un archivo por página seleccionada. | Limitado: no agrupa rangos en un archivo ni divide por tamaño o separadores. Elegir `1-3` exporta tres archivos. |
| Eliminar páginas | Quitar únicamente las páginas elegidas [S1]. | Conserva el resto y rechaza quitar todas o indicar páginas inexistentes. | Básico verificado. Conviene incorporar selección visual de eliminación. |
| Rotar PDF | Girar las páginas elegidas, conservando las demás [S1]. | Modifica `/Rotate` en 90, 180 o 270 grados. | Básico verificado. Anotaciones y documentos con rotaciones previas necesitan corpus adicional. |
| Imágenes a PDF | Crear páginas con imágenes en el orden elegido [S1]. | JPG, PNG y WebP, una imagen por página; dimensión calculada a 96 DPI. | Básico verificado con límites: no hay tamaño de papel/márgenes configurables; orientación EXIF, transparencia y perfiles de color no certificados. |
| PDF a PNG | Renderizar páginas como imágenes. | Exporta páginas a la resolución seleccionada y preserva su proporción en la imagen. | Básico verificado. Comparar colores, transparencia y anotaciones en corpus ampliado. |
| PDF a JPG | Renderizar con resolución y calidad JPEG elegidas [S1]. | Produce imágenes decodificables; JPEG tiene pérdida. | Básico verificado. No equivale a extraer las imágenes originales del PDF. |
| Extraer texto | Recuperar texto existente; OCR es otra operación [S9]. | Produce TXT con separadores de página; no reconoce imágenes. | Básico verificado. Orden de lectura complejo, ligaduras, columnas y texto invisible requieren pruebas adicionales. |
| Marca de agua | Añadir una marca conservando el contenido [S1]. | Inserta texto centrado; el texto original sigue extraíble. | Básico verificado en páginas normales. Solo texto, sin imagen ni controles de escala/posición; rotaciones, recortes y textos muy largos pendientes. |
| Numerar páginas | Aplicar numeración con inicio y alcance claros [S10]. | Usa `StartNumber + índice de página del documento`. Página 3, inicio 7, produce 9. | Desajuste de semántica. Propuesta: iniciar en 7 en la primera página seleccionada y numerar consecutivamente, o renombrar el campo como desplazamiento y explicar la regla actual. |
| Recortar PDF | Reducir la región visible; no borrar información [S3]. | Modifica CropBox; el marcador fuera de la caja visible sigue siendo extraíble. | Básico verificado y comportamiento correcto del recorte. No es una herramienta de privacidad/censura. |
| Metadatos | Editar propiedades y metadatos del documento [S11]. | Edita título/autor; limpiar elimina Info y XMP del catálogo. | Limitado: no es una limpieza forense de adjuntos, anotaciones, contenido ni metadatos de todos los objetos. No se ha certificado eliminación de toda información personal. |
| Proteger PDF | Exigir contraseña y cifrar el documento [S7]. | Aplica cifrado V5/AES-256; el archivo exige contraseña. | Básico verificado. No hay permisos configurables separados ni una contraseña de propietario distinta. |
| Quitar contraseña | Retirar protección disponiendo de la credencial necesaria [S1, S7]. | Abre con contraseña conocida y guarda sin cifrado. | Básico verificado en PDFs generados por AiOPDF. No promete romper contraseñas; documentos de terceros con restricciones de propietario pendientes. |
| Aplanar PDF | Hacer permanente contenido interactivo/capas; existen variantes de aplanado [S8]. | Rasteriza todas las páginas elegidas y pierde texto, enlaces y formularios. | Limitado y con error de geometría en PDFs recortados. No es aplanado selectivo de campos/anotaciones manteniendo el resto vectorial. |
| Comprimir PDF | Optimizar tamaño mediante recursos, imágenes y limpieza [S2]. | Rasteriza a JPEG; en la prueba aumentó de 20.727 a 143.520 bytes y eliminó texto seleccionable. | Desajuste con la expectativa de optimización. No mide ni evita el aumento, ni conserva texto. También tiene error de geometría en PDFs recortados. |
| Escala de grises | Cambiar color a grises. | El color se convierte a grises, pero todas las páginas elegidas se rasterizan. | Limitado y con error de geometría en PDFs recortados. No conserva vectores, texto ni estructuras interactivas. |
| Comparar PDFs | Revisar cambios; texto y apariencia son modos distintos [S6]. | Compara palabras por número de página, muestra añadidos/eliminados con contexto y exporta TXT/JSON; páginas sin texto quedan pendientes de OCR. | Básico verificado como comparación de texto. No alinea páginas movidas ni texto redistribuido entre páginas, no compara gráficos ni interpreta significado. La comparación por píxeles sí es válida para dibujos/escaneos; no era intrínsecamente incorrecta, sino un modo insuficiente para la expectativa de texto. |
| OCR a PDF | Producir un PDF buscable a partir de imágenes [S9]. | Reconoce con modelos latinos locales e inserta texto invisible sobre una imagen de página. | Básico verificado en muestras legibles; error de geometría en PDFs recortados. Calidad en fotografías, ruido, inclinación, escritura manual y corpus multilingüe pendiente. |
| OCR a texto | Reconocer caracteres y entregar texto [S9]. | Exporta TXT local; reconocimiento y extracción directa son diferentes. | Básico verificado en muestras simples. No conserva diseño ni garantiza exactitud; tablas, orden de lectura y calidad requieren evaluación con referencias humanas. |
| PDF a Word | Una conversión completa busca conservar diseño y contenido editable [S5]. | Exporta párrafos de texto a DOCX; no reconstruye tablas, imágenes, columnas ni estilos. | Exportación limitada, no conversión completa. Debe presentarse como texto de PDF a Word. Los escaneos requieren OCR previo. |
| Texto a Excel | El nombre actual promete exportar texto; una conversión de tablas sería otra función [S5]. | Genera columnas Página/Texto, una línea por fila. | Coherente con el nombre actual, limitado frente a PDF a Excel: no detecta tablas, tipos numéricos ni fórmulas. |
| PDF a diapositivas | La conversión editable y el pase de imágenes tienen objetivos diferentes [S5]. | Coloca una imagen de cada página en una diapositiva, conservando proporción. | Exportación de imágenes verificada. No reconstruye elementos editables; la interfaz debe mantener esa distinción. |
| Censurar región | Eliminar irreversiblemente la información elegida [S4]. | Pinta negro sobre un render y genera PDF nuevo sin texto original. Elegir página 1 de 3 genera solo una página. | Eliminación del texto original verificada en prueba de cobertura completa; alcance sorprendente y error de geometría en PDFs recortados. Propuesta: conservar por defecto las páginas no censuradas y permitir extraerlas solo como opción explícita. Pendiente: regiones parciales verificadas por render/OCR y limpieza completa de datos ocultos. |

## Hallazgos reproducibles y prioridades

1. **Geometría — error confirmado.** Después de recortar, la región visible mide 343,31 × 243,31 pt. Su render real es 720 × 510 px, con la misma proporción. Aplanar, comprimir, escala de grises, censura y OCR a PDF vuelven a colocar esa imagen en una página de 400 × 300 pt: cambian su proporción. Deben utilizar la geometría visible y respetar rotación. También hay que comprobar CropBox no centradas y orígenes no nulos.
2. **Censura — decidir el contrato de alcance.** Una operación llamada censurar una página no debería extraer implícitamente esa página bajo el contrato propuesto para AiOPDF. El código y la ayuda actual sí anuncian selección exclusiva, por lo que es una decisión funcional problemática, no un fallo de selección del motor.
3. **Numeración — resolver la ambigüedad.** “Número inicial” no explica que se suma el índice físico de página. La prueba página 3/inicio 7 da 9. Se necesita numeración consecutiva de las páginas elegidas o explicar expresamente el desplazamiento.
4. **Compresión — sustituir el mecanismo y medir el resultado.** El aumento observado es de aproximadamente 6,92 veces. No todos los PDFs admiten reducción, pero el motor debe informar del cambio real y evitar presentar un archivo mayor como comprimido con éxito. Es necesario optimizar recursos preservando texto cuando sea posible; rasterizar debe ser una opción diferenciada.
5. **Aplanado y Office — no prometer fidelidad inexistente.** Aplanar por rasterizado y exportar texto/imágenes son funciones válidas con límites, pero no sustituyen aplanado selectivo o reconstrucción editable del documento.
6. **Extender validación.** Introducir fixtures de formularios, anotaciones, enlaces internos, marcadores, PDF firmados, transparencias, EXIF, idiomas, columnas, páginas movidas y escaneos difíciles. Mantener un conjunto de referencia con propiedades esperadas antes de marcar cada capacidad avanzada como verificada.

No se modificó el motor durante esta auditoría. Los problemas descritos siguen presentes en 0.5.0. Los resultados anteriores de pruebas de generación/apertura no deben interpretarse como aprobación de estos comportamientos.

## Evidencia local

- Medidas: [measurements.md](../artifacts/behavior-audit/measurements.md).
- Archivos del ensayo más reciente: `artifacts/behavior-audit/run-76faebf1182c40bcb21c7ebe2b9fb8d1`.
- Prueba reproducible: `dotnet run --project tests/AiOPDF.Checks -c Release -- --audit`.
- Código del ensayo: `tests/AiOPDF.Checks/BehaviorAuditChecks.cs`. El ensayo **registra hallazgos**, no declara que esos resultados sean correctos.
- Base de pruebas existente: 43 comprobaciones de motor, 16 de comparación, 50 flujos de procesamiento y 100 combinaciones de herramienta/tamaño. Cubren el alcance especificado en `docs/VALIDATION.md`; faltan las capacidades avanzadas enumeradas en la matriz.
- Los originales del ensayo permanecieron idénticos byte por byte. Los datos son sintéticos. No se enviaron documentos a los productos de referencia.

## Documentación primaria consultada

- S1: [iLovePDF: guía de uso de las herramientas](https://www.ilovepdf.com/help/documentation).
- S2: [Adobe: opciones de optimización de PDF](https://helpx.adobe.com/acrobat/desktop/create-documents/optimize-pdfs/pdf-optimizer-settings.html).
- S3: [Adobe: recortar páginas y contenido oculto](https://helpx.adobe.com/ca/acrobat/classic/crop-pdf-pages.html).
- S4: [Adobe: eliminar información sensible](https://experienceleague.adobe.com/en/docs/document-cloud-learn/acrobat-learning/advanced-tasks/protect/redact).
- S5: [Adobe: exportar PDF conservando diseño y editabilidad](https://www.adobe.com/learn/acrobat/web/convert-pdf-retain-page-layout).
- S6: [Adobe: comparar documentos y elegir modos de comparación](https://helpx.adobe.com/acrobat/using/compare-documents.html).
- S7: [PDFsharp: métodos de cifrado](https://docs.pdfsharp.net/PDFsharp/Topics/PDF-Features/Encryption.html).
- S8: [Adobe: combinar y aplanar capas](https://helpx.adobe.com/acrobat/desktop/edit-documents/pdf-layers/merge-layers.html).
- S9: [Adobe: reconocer texto en documentos escaneados](https://helpx.adobe.com/acrobat/desktop/create-documents/scan-documents-to-pdfs/recognize-text.html).
- S10: [Adobe: encabezados, pies y numeración](https://helpx.adobe.com/acrobat/desktop/edit-documents/use-headers-and-footers/add-numbers.html).
- S11: [Adobe: editar metadatos](https://helpx.adobe.com/acrobat/desktop/edit-documents/edit-pdf-properties/edit-metadata.html).
