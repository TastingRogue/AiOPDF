# Auditoría para AiOPDF 1.0

Fecha: 13 de octubre de 2026. Base examinada: 0.6.4, con el icono aprobado integrado. Dictamen: todavía no publicar como 1.0 multiplataforma. La cobertura funcional existe, pero falta demostrar fiabilidad, privacidad y distribución en los sistemas objetivo. Esta auditoría revisa código, suites existentes, metadatos y paquetes; no es una prueba de penetración ni certificación.

## Alcance propuesto para 1.0

Las 25 herramientas actuales, con sus límites explícitos, procesamiento local, conservación de originales y revisión antes de guardar. No necesita paridad completa con iLovePDF. No necesita servidor localhost ni servicios externos. Las exportaciones básicas Office, censura rasterizada y comparación de texto deben conservar sus nombres y límites honestos.

## Evidencia obtenida

- Motor general: 44 mensajes PASS, artifacts/audit-1.0-general.log.
- Comparación: 24 comprobaciones, artifacts/audit-1.0-comparison.log.
- Auditoría funcional: terminó sin error y conservó originales, artifacts/audit-1.0-audit.log; documentos en artifacts/behavior-audit/run-d13040d9b49b47a0a1f7447f14a8445f.
- Mejoras: 129 comprobaciones, artifacts/audit-1.0-improvements.log.
- Interfaz: terminó sin error; 25 herramientas en cuatro tamaños y 50 flujos A/B, artifacts/audit-1.0-ui.log. Sus 485 mensajes PASS incluyen verificaciones parciales; no equivalen a 485 pruebas independientes.
- Consulta NuGet directa/transitiva: ningún paquete vulnerable reportado por la fuente consultada. artifacts/audit-1.0-vulnerabilities.log. No cubre exhaustivamente CVE de PDFium/Skia/ONNX ni demuestra ausencia de vulnerabilidades.
- Publicación Windows/macOS/Linux: builds cruzados; no ejecución nativa Mac/Linux.
- Ejecutable Windows publicado: Authenticode NotSigned.

## Bloqueos de lanzamiento

P1 indica trabajo necesario antes de anunciar una 1.0 general. No se encontró evidencia suficiente para declarar un fallo crítico de pérdida de originales en las pruebas realizadas.

| ID | Prioridad | Hallazgo y evidencia | Acción y criterio de cierre |
|---|---|---|---|
| PRIV-01 | P1 | PreparedResult.cs:23–38 limpia únicamente directorios de la sesión al cerrar normalmente. No hay recuperación de temporales de sesiones interrumpidas; las copias procesadas pueden contener documentos privados. | Directorios privados con permisos apropiados por plataforma, registro de propiedad y limpieza de sesiones abandonadas sin tocar sesiones activas. Probar cierre normal, terminación forzada y reinicio. Explicar retención; no prometer borrado forense. |
| PERF-01 | P1 | ReviewWorkspace.cs:94 y PreparedResult.cs:83 agregan bitmaps nuevos a previewImages en cada navegación; se liberan en DisposePreviews, no al sustituir la página. La construcción de detalles de comparación también es completa y anticipada en MainWindow.cs:389. | Liberar imágenes sustituidas, limitar caché y cargar detalles bajo demanda. Navegar repetidamente en PDFs de 500 páginas; después del calentamiento la memoria debe estabilizarse y el visor mantenerse usable. |
| PERF-02 | P1 | SavePreparedToPath en PreparedResult.cs:137–141 hace File.Copy síncrono desde la interfaz. Lectura/renderizado nativo y algunas previsualizaciones no tienen cancelación interna ni presupuesto de recursos. | Copiar fuera del hilo de UI, mostrar progreso y definir cancelación y límites de entrada/renderizado. Probar PDF grande, disco lleno, acceso denegado, red desconectada en carpeta destino y cancelación sin salida parcial. Mantener arquitectura de escritorio sin localhost. |
| QA-01 | P1 | Las muestras sintéticas y pruebas existentes no constituyen un corpus de compatibilidad amplio ni pruebas prolongadas de memoria/OCR. | Corpus versionado y autorizado: formularios, enlaces, marcadores, PDF firmado, cifrado, corrupto, Unicode, columnas, CropBox/rotación, EXIF, imágenes enormes y escaneos difíciles. Documentar expectativas por herramienta y verificar visualmente resultados. |
| CMP-01 | P1 | ContentComparison.cs:48 usa ContentOrderTextExtractor; ComparisonMarkup.cs:16 usa GetWords y ejecuta otro diff. Son dos fuentes de secuencia diferentes. No se ha demostrado que coincidan siempre en columnas y orden de contenido irregular. | Unificar tokens, ocurrencias y coordenadas del informe y subrayado, o demostrar consistencia con corpus. Cada cambio del informe debe corresponder a marcas correctas; texto igual no debe marcarse por reordenamiento de extracción. |
| DIST-01 | P1 | Mac/Linux solo compilados desde Windows; scripts de empaquetado no ejecutados en esos sistemas. Última prueba histórica de instalar/desinstalar Windows es 0.5.1, no 0.6.4. | Instalar, arrancar, procesar, guardar, actualizar y desinstalar en máquinas limpias Windows, macOS Intel/ARM y Linux objetivo. Validar dependencias gráficas y modelos OCR incluidos. Declarar versiones mínimas soportadas. |
| DIST-02 | P1 para distribución pública | Windows sin firma. El script Mac admite codesign opcional, pero no incluye envío a notarytool ni stapling. Iconset carece de variantes @2x explícitas. | Firmar ejecutable/instalador Windows cuando se disponga de certificado; validar política de distribución. Completar iconset, firma Developer ID, notarización y ticket Mac. No publicar como listo para Gatekeeper hasta probarlo. |
| RELEASE-01 | P1 | No se encontró CI de publicación en los archivos del proyecto examinados ni licencia principal de distribución. ROADMAP anterior describía 0.2 y comportamiento obsoleto. | Una fuente de versión, builds reproducibles con lockfiles, suites y auditoría de dependencias, inventario/hash de paquetes, política de licencia del producto, changelog y checklist de release. Verificar avisos y modelos contra binarios efectivamente entregados. |

## Mejoras importantes y decisiones de producto

- P2: pruebas reales de teclado, foco, lector de pantalla y escalado 125/150/200%; los controles personalizados y Headless no certifican accesibilidad nativa.
- P2: recuperación de errores consistente y diagnósticos locales optativos sin contenido de documentos ni contraseñas; no introducir telemetría obligatoria.
- P2: manejo explícito de PDFs firmados: advertir/verificar efectos de modificaciones sobre las firmas existentes.
- P2: la entrada MimeType PDF del desktop Linux no lleva %f y el programa no consume archivos de inicio. Si se promete abrir con doble clic, implementarlo y probarlo; no bloquear 1.0 si se excluye del alcance.
- P2: limitar acumulación de temporales entre operaciones en sesiones largas y permitir descartarlos conservando los que alimentan otra herramienta.
- P2: evaluar calidad del icono a 16 px y gran escala en la shell real; el máster aprobado es raster, no vector editable. app-icon.svg es una referencia antigua y no la fuente actual.

## Funciones que pueden esperar a 1.1+

Office a PDF, reconstrucción avanzada de tablas/diseño, PDF/A, reparación, edición de formularios, firma digital, compresión con pérdida avanzada, censura vectorial, múltiples regiones, comparación de imágenes y alineación de páginas movidas. Solo serían requisitos de 1.0 si se anuncian expresamente como parte de esa versión.

## Orden de trabajo

1. Cerrar PRIV-01, PERF-01 y PERF-02; añadir regresiones de cierre inesperado y navegación prolongada.
2. Cerrar CMP-01 y QA-01; fijar contratos y límites de las 25 herramientas.
3. Cerrar DIST-01/DIST-02 por plataforma y RELEASE-01; realizar beta con usuarios y documentos autorizados.
4. Congelar funciones, ejecutar corpus y checklist sobre candidatos firmados; publicar 1.0 solo cuando no queden P1 abiertos para las plataformas anunciadas.

Criterio final: originales intactos, guardado sin sobrescrituras/archivos parciales, temporales controlados, UI responsive con memoria limitada, comparación coherente y paquetes instalados/validados en cada plataforma anunciada. No se asigna una fecha artificial: faltan equipos y certificados para parte de la validación.

## Fuentes externas

[Auditoría NuGet](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages): alcance y tratamiento de avisos de dependencias. [Notarización Apple](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution): distribución macOS firmada y comprobada. Estas fuentes sustentan los criterios de distribución; los hallazgos de código provienen del proyecto local.

## Seguimiento 2026-10-13

La candidata 1.0.0-rc.1 aplica las correcciones de temporales, memoria, guardado y comparación, y prepara el proceso de release. El detalle y las evidencias están en [PROGRESS-1.0-RC1.md](PROGRESS-1.0-RC1.md). Los requisitos de distribución nativa, corpus amplio y firma siguen abiertos; esta auditoría conserva sus hallazgos originales como referencia.

