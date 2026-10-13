# Icono de aplicación · 0.6.3

Marca propia: una hoja blanca con esquina doblada y un escudo turquesa con cerradura sobre un fondo violeta. Representa herramientas para documentos y privacidad. Mantiene los colores de la interfaz y sustituye el icono anterior de la ventana, ejecutable, instalador y encabezado.

Fuente editable: src/AiOPDF/Assets/app-icon.svg. PNG de 16, 20, 24, 32, 40, 48, 64, 128, 256, 512 y 1024 px. ICO con nueve tamaños de 16 a 256 px para que Windows elija el adecuado sin reducir siempre una única imagen. Generador reproducible en tests/AiOPDF.Checks/AppIconAssets.cs (--assets). Vista de tamaños sobre fondos claro y oscuro: docs/screenshots/app-icon-sizes.png.

Validación: generación de recursos, compilación y suite --ui-preview con las 25 herramientas en cuatro tamaños. Revisión visual del encabezado y de los tamaños reales del icono. Linux empaqueta el PNG de 256 px; macOS dispone del original de 1024 px para generar el iconset. La ejecución nativa de los paquetes macOS/Linux requiere esos sistemas.
