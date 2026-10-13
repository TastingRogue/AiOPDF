# Revisión de interfaz · 0.6.1

| Prioridad | Componente | Antes | Corrección |
|---|---|---|---|
| Alta | StudioTheme.axaml: Button / ButtonSpinner | Los signos de los controles numéricos quedaban a la izquierda | Centrado horizontal y vertical; botones de 42 × 44 y sin relleno |
| Media | ReviewWorkspace.cs: región | Etiqueta, selector y ámbito sin separación ni alineación | Agrupar etiqueta y número; centrar verticalmente y envolver con separación de 12 px |
| Media | ReviewWorkspace.cs / PreparedResult.cs | Acciones de comparación y revisión pegadas | Barra compartida con separación horizontal y espacio entre filas |
| Media | MainWindow.cs: archivos | Los nombres largos podían envolver y aumentar la altura de la fila | Una línea con puntos suspensivos y ruta completa en tooltip |
| Media | StudioTheme.axaml: ComboBox / CheckBox | Contenido del selector sin recorte; marca desalineada | Recorte del contenido dentro del relleno; marca centrada de 16 px |

Validación: --ui completó las 25 herramientas en cuatro tamaños (640, 1280, 1920 y 2560 px), navegación y 50 flujos de salida. Se añadieron comprobaciones de centrado de steppers y de solapamiento en las barras compartidas. --improvements completó 121 comprobaciones de procesamiento, revisión, comparación, selección de región y guardado. Capturas renderizadas en docs/screenshots; revisión visual de censura a 1280 px confirma el cambio señalado por el usuario.

Aprobación del alcance: correcciones comprobadas en el renderizador Avalonia y sus pruebas de interacción. Esto no certifica todos los estados posibles ni una ejecución nativa en macOS/Linux. Los cambios conservan las plantillas propias, sin introducir componentes visuales predeterminados.

Evidencia: artifacts/ui-alignment.log y artifacts/improvements-ui-0.6.1.log.
