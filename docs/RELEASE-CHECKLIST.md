# Puerta de salida para 1.0 estable

No cambiar VERSION a 1.0.0 hasta completar y guardar evidencia de los puntos abiertos.

- [x] MIT elegida por el propietario y archivo LICENSE incluido.
- [x] Fuente única VERSION y lockfiles.
- [x] Recuperación de temporales con sesión activa protegida; prueba de terminación forzada.
- [x] Bitmaps limitados durante navegación en documento de 500 páginas.
- [x] Guardado cancelable y sin sobrescritura con limpieza de staging.
- [x] Misma secuencia y diff para informe y subrayado.
- [x] Límites de renderizado y compilación sin advertencias.
- [x] Workflow nativo del motor e interfaz pasó en Windows/macOS/Linux: https://github.com/TastingRogue/AiOPDF/actions/runs/38428372534 (commit 427fb8b, RC3).
- [x] Instalación, motor/OCR instalado, arranque UI, reinstalación de la misma versión y retirada en runners nativos Windows x64, Ubuntu 24.04 amd64 y macOS 15 arm64: https://github.com/TastingRogue/AiOPDF/actions/runs/38428372557 (commit 427fb8b).
- [ ] Aceptación humana en equipos limpios, incluidos actualización entre versiones y comportamiento al descargar paquetes sin firma/notarización. Reinstalar el mismo build no demuestra actualización.
- [ ] Firmar EXE e instalador Windows con certificado del propietario.
- [ ] Generar y firmar DMG; ejecutar notarización/stapling y validarlo en Mac.
- [ ] Ampliar corpus de aceptación con formularios, documentos firmados, escaneos variados, EXIF y datos autorizados.
- [ ] Probar disco lleno, destino de red interrumpido, permisos y límites bajo carga real.
- [ ] Probar teclado, lector de pantalla y escalado 125/150/200% en sistemas reales.
- [ ] Revisar avisos de componentes nativos y modelos frente a los binarios entregados.
- [ ] Congelar funciones, beta de usuarios, cero P1 abiertos y hashes de paquetes finales.

Procesamiento nativo: la cancelación se comprueba entre operaciones; una llamada nativa de renderizado ya iniciada puede tardar en devolver el control. Los límites de recursos no son un aislamiento contra todo PDF malicioso.

No se necesita servidor, localhost ni transmisión de documentos. Las pruebas de CI usan datos sintéticos. Las firmas requieren certificados que no se incorporan al repositorio ni se solicitan por chat.

## Instaladores

Consultar [INSTALLERS.md](INSTALLERS.md) para construcción, pruebas y requisitos de firma. No hay certificados del propietario disponibles todavía. Los resultados de CI no sustituyen la aceptación humana de accesibilidad y compatibilidad.

