# Dirección visual — AiOPDF 0.4

Caja de herramientas de papel para documentos personales. Ilustración vectorial propia, navegación lavanda y acento amarillo; área de trabajo tranquila y legible.

Tokens: papel #FFFFFF, fondo #FCFAF6, tinta #29243D, texto secundario #675F77, acción #5A3FC0, navegación #EEE7FF, ilustración #FFF0C3. Lato regular y bold incorporados. Alineación izquierda y colores de categoría conservados.

Categorías laterales en ventanas amplias y filtros en inicio bajo 1000 px. Se conservan búsqueda, categoría y documentos compatibles al navegar. Ctrl+F enfoca búsqueda; Escape vuelve al inicio. Guardar aparece también en el encabezado en ventanas estrechas.

StudioTheme.axaml define plantillas visuales propias para botones, campos, selección de páginas, listas, controles numéricos, desplegables, barras y ayudas. Avalonia aporta comportamiento y semántica; los selectores de archivo del sistema siguen siendo nativos. DeskArt.cs contiene la ilustración original.

Contraseña y calidad se revelan a petición. El flujo indica añadir, ajustar y guardar una copia. Selección y foco tienen señales adicionales al color. No hay animaciones decorativas automáticas.

Aplicadas frontend-design de Anthropic y better-* de jakubkrehel. Investigación en UX-RESEARCH.md. Capturas e interacción verificadas automáticamente; pendiente evaluación con usuarios y lectores de pantalla físicos.

### Panel de resultado · 0.4.3
Cabecera lavanda con el icono vectorial de la herramienta, instrucciones de guardado explícitas y recibo amarillo con icono propio. El recibo muestra la ruta real del resultado y la acción para abrir su carpeta. Se reemplazó la ilustración 3D por el mismo lenguaje vectorial del resto de la interfaz. Captura: screenshots/result-panel.png.


### 0.4.6 · Distribución responsiva
Inicio: entre dos y siete columnas según el ancho; búsqueda debajo del título en ventanas pequeñas; cabecera más baja en pantallas amplias. Herramientas: opciones de 420 px en escritorio y columna única bajo 900 px útiles. El ancho restante pertenece a la vista previa; las páginas escalan con el ancho y la altura de la ventana, con renders de 720 px. Títulos y descripciones se ajustan al ancho. Menú lateral desplazable en ventanas de poca altura. Se mantiene desplazamiento vertical para documentos largos y opciones extensas.


### 0.5.0 · Acciones y revisión de contenido
Las 25 acciones primarias ahora nombran la operación y el resultado (Unir y guardar, Exportar a Word, etc.). La comparación usa un flujo propio: elegir dos versiones, comparar en memoria y exportar informe opcionalmente. Su panel reemplaza la vista previa de un solo PDF por el resumen y cambios con contexto en dos columnas. Original/eliminado aparece en rosa; actualizado/añadido, en verde; ambos tienen etiquetas textuales. Eliminar páginas exige selección explícita; proteger exige ocho caracteres; marca de agua exige texto. Limpiar metadatos desactiva título y autor para evitar entradas ignoradas.

