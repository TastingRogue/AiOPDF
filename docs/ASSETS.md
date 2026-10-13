# Recursos visuales originales

Los 25 SVG en src/AiOPDF/Assets/icons y el icono PNG/ICO de la aplicación se crearon mediante código propio (BrandIcon.cs y AssetChecks.cs). No se descargaron paquetes de iconos ni marcas de iLovePDF o Stirling.

Las imágenes siguientes se generaron con la herramienta integrada image_gen, modo generate, sin referencias externas. Se copiaron al proyecto para que no dependan de URLs ni archivos temporales.

## privacy-hero.png

Archivo: src/AiOPDF/Assets/privacy-hero.png. Ilustración principal de documentos y escudo.

Prompt: Use case: stylized-concept. Asset type: original illustration for AiOPDF offline desktop PDF application. Create a polished minimal editorial 3D paper illustration: a small stack of ivory documents with folded corners protected by a rounded teal shield with subtle check mark, warm orange accent tabs, soft shadows. Pale warm ivory background, generous negative space, no text, no logos, no watermark. Landscape composition, calm trustworthy approachable style, legible at small size. This is decorative onboarding artwork, not a screenshot.

## success.png

Archivo: src/AiOPDF/Assets/success.png. Ilustración de resultado guardado.

Prompt: Use case: stylized-concept. Asset type: original success illustration for AiOPDF offline desktop PDF application. A single warm ivory folded paper document with a small embossed teal checkmark badge, a tasteful orange bookmark tab and two tiny paper confetti pieces. Soft polished clay-like 3D render, approachable and quiet, pale warm ivory background, centered isolated composition with wide margins. No text, no logos, no watermark. Match a friendly teal, ivory and orange document productivity application.

Fuente Lato regular y bold incorporada bajo SIL Open Font License, con aviso Lato-OFL.txt. Capturas propias de la aplicación en docs/screenshots, obtenidas con Avalonia.Headless; no son mockups generados.


La versión 0.4 agrega DeskArt.cs: ilustración vectorial original de documentos y lápiz dibujada en Avalonia, sin descargas ni recursos de terceros. StudioTheme.axaml contiene las plantillas visuales originales de los controles.

0.4.1: iconos de función ampliados a 44 unidades en tarjetas. Unir, dividir, organizar y comparar usan composiciones distintas de páginas; imágenes y exportación tienen sentido de conversión; Word, texto y OCR a texto tienen símbolos diferentes; desbloquear muestra candado abierto. SVG exportados desde las mismas geometrías que dibuja la aplicación.

0.6.4: el icono aprobado se generó/editó con image_gen y usa transparencia. Máster: branding/aiopdf-icon-master.png. AppIconAssets.cs exporta PNG e ICO desde ese máster; ya no dibuja el escudo anterior. app-icon.svg se conserva como referencia antigua y no se utiliza como fuente. Prompts de la exploración en output/branding/DESIGN-RESEARCH.md.
