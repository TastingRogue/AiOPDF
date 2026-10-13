# Candidata AiOPDF 1.0.0-rc.1

Fecha: 2026-10-13. Licencia del código: MIT, elegida por el propietario. Esta es una candidata de validación; no certifica todavía una 1.0 estable multiplataforma.

## Trabajo aplicado a la auditoría

| Hallazgo | Resultado en esta candidata | Pendiente |
| --- | --- | --- |
| PRIV-01 | Sesiones privadas con permisos restringidos, lease exclusivo y recuperación de sesiones abandonadas. Terminación real de un proceso hijo probada. | Revisar comportamiento bajo fallo de disco y permisos reales por plataforma. |
| PERF-01 | Liberación de bitmaps al navegar; detalles de comparación construidos al expandirlos. Prueba con 500 páginas y 40 transiciones: máximo dos imágenes de comparación vivas. | Perfil de memoria y uso prolongado nativo en Mac/Linux. |
| PERF-02 | Copia asíncrona cancelable, staging propio, progreso y publicación final sin sobrescritura. Límites de entrada, páginas y rasterizado. | Disco lleno, cortes de red y carga real. Una llamada nativa iniciada puede no cancelarse inmediatamente. |
| CMP-01 | Informe y subrayados consumen la misma secuencia de palabras y el mismo diff por página. | Validación con corpus más amplio. |
| QA-01 | Regresiones de las 25 herramientas y corpus sintético adicional: acentos, columnas y PDF corrupto. | Formularios, firmas, escaneos variados, EXIF y aceptación humana. |
| RELEASE-01 | VERSION única, lockfiles, SDK fijado, CI en tres sistemas, licencia MIT, manifiestos SHA-256 y checklist. | Ejecutar CI hospedada y verificar avisos nativos/modelos contra los paquetes finales. |
| DIST-01 / DIST-02 | Instalador candidato Windows y scripts de empaquetado/firma/notarización preparados. | Instalación limpia por sistema, certificados, firma y notarización reales. |

## Evidencia local

Compilación Release con advertencias tratadas como errores: artifacts/rc-build.log.

Pruebas ejecutadas satisfactoriamente:

- Operaciones generales: artifacts/rc-general.log.
- Comparación: artifacts/rc-comparison.log.
- Auditoría de comportamiento: artifacts/rc-audit.log.
- Flujos revisados y guardado asíncrono: artifacts/rc-improvements.log.
- Interfaz, navegación y flujos de las 25 herramientas: artifacts/rc-ui.log.
- Recuperación después de terminación forzada y copia cancelable: artifacts/rc-readiness.log.
- Comparación de 500 páginas y navegación prolongada: artifacts/rc-stability.log.
- Corpus adicional: artifacts/rc-corpus.log.

Estas pruebas se ejecutaron en Windows; las pruebas de interfaz automatizadas usan Avalonia Headless y no sustituyen pruebas de accesibilidad o instalación nativas. No se ha ejecutado el workflow hospedado ni se han instalado estos paquetes en Mac/Linux.

## Artefacto para revisar

artifacts/installers/AiOPDF-1.0.0-rc.1-windows-x64-setup.exe. Ejecutable autónomo en artifacts/windows-x64-1.0.0-rc.1/AiOPDF.exe. Manifiesto del contenido en release-manifest.json dentro de ese paquete. El instalador no está firmado.

La publicación estable queda condicionada a docs/RELEASE-CHECKLIST.md. No cambiar VERSION a 1.0.0 ni anunciar validación multiplataforma antes de completar esas evidencias.

Empaquetado Windows completado con Inno Setup. Arranque del ejecutable autónomo comprobado (artifacts/rc-packaged-smoke.log). Versión de producto: 1.0.0-rc.1; manifiesto de contenido: 309 archivos. Hash SHA-256 del instalador: B8F26D234C1BF2A40E8289DFF1A6D288CEB1A82A2E8004A2C0212C8DBB24C4A8. Registro del paquete: artifacts/installers/AiOPDF-1.0.0-rc.1-windows-x64-setup.manifest.json.

