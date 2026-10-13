# AiOPDF 1.0.0-rc.1

Aplicación de escritorio en español para procesar PDF localmente. C#/.NET 10 y Avalonia, sin servidor HTTP, navegador ni localhost. El usuario instala una aplicación; las bibliotecas nativas y modelos OCR vienen incluidos.

## Descargar y probar

Instalador candidato Windows x64: `artifacts/installers/AiOPDF-1.0.0-rc.1-windows-x64-setup.exe`. Instalación por usuario sin exigir SDK ni permisos de administrador. También se puede abrir `artifacts/windows-x64-1.0.0-rc.1/AiOPDF.exe` directamente. Esta candidata aún no es la versión estable: ver docs/PROGRESS-1.0-RC1.md y docs/RELEASE-CHECKLIST.md.

## Funciones implementadas

25 herramientas con selección y arrastre de archivos, búsqueda, categorías, miniaturas, orden visual, progreso y cancelación:

- Unir, organizar/extraer/duplicar, dividir por páginas, rangos o grupos, eliminar y rotar.
- JPG/PNG/WebP a PDF; PDF a JPG/PNG; extracción de texto.
- Marca de agua, numeración, recorte y edición o limpieza de metadatos.
- Cifrado AES-256, eliminación de contraseña conocida y censura de una región.
- Rasterizado, compresión sin pérdida, escala de grises y comparación del texto de dos versiones.
- OCR local latino a texto o PDF buscable.
- Exportaciones básicas a Word, Excel y diapositivas.

Los originales se conservan. Los resultados se guardan con nombre nuevo y no se sobrescriben archivos existentes. La acción prepara una copia temporal para revisar; Guardar copia elige el destino final. Los temporales de revisión se eliminan al cerrar normalmente; tras una interrupción, las sesiones abandonadas se limpian al volver a iniciar sin borrar sesiones activas. Una salida PDF puede continuar en otra herramienta.

## Límites actuales

La compresión optimiza sin rasterizar; si no reduce el tamaño, guarda una copia idéntica y lo indica. Rasterizado, escala de grises y censura convierten las páginas seleccionadas en imágenes: pierden texto, formularios y enlaces en esas páginas y conservan las demás. OCR a PDF también conserva las páginas no seleccionadas. El recorte solo oculta márgenes. Texto a Word exporta párrafos; Páginas a diapositivas exporta imágenes, sin reconstruir contenido editable.

Word exporta texto sin conservar el diseño; Excel coloca líneas en filas sin reconstruir tablas; diapositivas contiene imágenes no editables. OCR usa modelos locales de alfabeto latino y necesita revisión humana. Comparar PDFs compara palabras por página: ignora espacios y saltos de línea, muestra añadidos/eliminados con contexto y permite exportar informes TXT/JSON. No compara imágenes ni interpreta el significado; las páginas sin texto extraíble quedan pendientes de OCR. No alinea automáticamente páginas desplazadas entre versiones. En páginas muy largas, los grandes bloques modificados se muestran como reemplazos para limitar memoria. Dividir permite un PDF por página, por rango o cada N páginas.

Aún faltan conversión de Office a PDF, reconstrucción avanzada de Office, firmas digitales, edición de formularios, PDF/A, reparación y otras funciones del inventario en ROADMAP.md. No se afirma paridad completa con iLovePDF o Stirling-PDF.

## Plataformas y validación

En Windows pasaron las comprobaciones de operaciones y los 25 flujos de guardado desde la interfaz, además de navegación/renderizado. El ejecutable empaquetado inició y no mostró conexiones TCP ni endpoints UDP durante la observación. Esto no sustituye una auditoría de red ni pruebas con un corpus amplio de documentos reales.

Se generaron paquetes autónomos para Windows x64, macOS Intel/Apple Silicon y Linux x64/ARM64. Mac y Linux aún no se ejecutaron en hardware de esos sistemas. Los scripts generan DMG en Mac y DEB inicial para Debian 13 en Linux; requieren validación, y macOS requiere firma y notarización para distribución normal.

## Compilar

SDK local: `.tools/dotnet/dotnet.exe`; también se puede usar un SDK .NET 10 instalado.

```powershell
& ./.tools/dotnet/dotnet.exe run --project src/AiOPDF
& ./.tools/dotnet/dotnet.exe run --project tests/AiOPDF.Checks -c Release
& ./.tools/dotnet/dotnet.exe run --project tests/AiOPDF.Checks -c Release -- --ui
& ./scripts/publish.ps1 -Runtime win-x64
& ./.tools/inno/tools/ISCC.exe packaging/windows.iss
```

En Mac: `bash scripts/package-macos.sh arm64` o `x64`. En Linux: `bash scripts/package-linux.sh x64` o `arm64`. Los scripts requieren SDK .NET y las herramientas de empaquetado del sistema. La aplicación instalada no requiere SDK.

## Diseño y dependencias

Interfaz original, 25 iconos vectoriales propios, icono de aplicación e ilustraciones generadas para AiOPDF. Ver docs/ASSETS.md y docs/screenshots. No se copió código de Stirling-PDF ni de iLovePDF. El motor excluido de Stirling tiene condiciones diferentes de su licencia general.

Bibliotecas principales: Avalonia, PDFsharp, PDFium/PDFtoImage, PdfPig, Skia, RapidOcrNet/ONNX Runtime y Open XML SDK. Licencias y avisos en docs/THIRD-PARTY-NOTICES.md y docs/licenses; se incluyen en los paquetes.





El rediseño 0.4 añade controles visuales propios, navegación lateral y una ilustración vectorial original. Ver docs/DESIGN.md y docs/UX-RESEARCH.md. Los diálogos de archivos siguen siendo los del sistema operativo.



PDF de prueba incluidos en output/pdf/AiOPDF-prueba-A.pdf y AiOPDF-prueba-B.pdf. Los 25 flujos de la interfaz se ejecutaron con cada documento (50 ejecuciones), usando imágenes derivadas de sus páginas para Imágenes a PDF. Contienen tablas ficticias, color, orientación horizontal y una página escaneada. Ver docs/VALIDATION.md.





La versión 0.6.0 añade revisión antes de guardar, continuación con resultados, división por rangos, selección visual de regiones y visor paralelo de comparación. Ver [implementación y validación](docs/REPORT-IMPROVEMENTS.md).


Estado para lanzamiento: [auditoría de versión 1.0](docs/AUDIT-1.0.md).

## Licencia

El código de AiOPDF se distribuye bajo MIT (LICENSE). Las bibliotecas, modelos y recursos conservan sus licencias respectivas; ver los avisos incluidos.

