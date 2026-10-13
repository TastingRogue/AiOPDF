# Comparación sobre páginas · 0.6.2

El visor presenta primero las dos páginas completas. A 100% ajusta ancho y alto al espacio disponible; el zoom permite ampliar y desplazarse. El texto eliminado se subraya en rosa en el original y el añadido en verde en la versión actualizada. Los PDFs de entrada no se modifican.

ComparisonMarkup utiliza la secuencia de palabras y sus coordenadas extraídas con PdfPig. Aplica el diff por contenido a esas ocurrencias; no busca todas las repeticiones de una palabra ni compara píxeles. Las líneas siguen la orientación del borde inferior de cada palabra. La imagen renderizada incluye los subrayados. Las páginas sin texto legible conservan la indicación de OCR; no reciben marcas que puedan interpretarse como una comparación completa.

Validación: 24 comprobaciones de comparación, incluyendo texto sin cambios con diferente formato, importes, ocurrencias repetidas, líneas visibles y coordenadas en páginas giradas. La suite de mejoras comprueba ambas páginas completas sin desbordamiento interno a 640, 1280, 1920 y 2560 px, navegación por cambios, zoom, procesamiento y guardado. Evidencia en artifacts/comparison-markup-checks.log y artifacts/comparison-full-page-checks.log. Capturas en docs/screenshots/comparison-full-page-*.png.

Límites existentes: compara texto por número de página, sin alinear páginas trasladadas ni interpretar imágenes, gráficos o significado. La exportación conserva el informe textual/JSON; los subrayados aparecen en el visor y no alteran los PDFs originales.
