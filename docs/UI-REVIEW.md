# Revisión de interfaz con jakubkrehel/skills

Fecha: 12 de octubre de 2026. Skills instaladas desde https://github.com/jakubkrehel/skills: better-interface, better-accessibility, better-layout, better-writing, better-typography, better-colors y better-ui.

## Alcance y cobertura

Inicio, búsqueda/filtros y flujo seleccionar herramienta → añadir documento → ajustar opciones → guardar. Stack existente C#/Avalonia/Fluent; sin cambio de motor ni incorporación de tecnologías web. No se encontraron AGENTS.md, CLAUDE.md ni una guía de diseño propia. Se conservaron el tono español, la paleta y los recursos originales.

| Dominio | Evidencia inspeccionada | Resultado |
|---|---|---|
| Accessibility | MainWindow, nombres de campos, foco por teclado, captura keyboard-focus | Nombres y foco comprobados parcialmente; lector de pantalla no verificado |
| Layout | Inicio y pantallas de herramientas a 1220, 860 y 640 DIP | Reflujo a una columna; tarjetas adaptables; altura mínima de tarjetas |
| Writing | Etiquetas, filtros sin resultados, instrucciones y errores | Restablecer filtros y pasos para recuperarse de errores |
| Typography | Label compartido y capturas | Interlineado proporcional por función y tamaño compacto de titular |
| Colors | Valores declarados y superficies de las capturas | Contraste medido de texto secundario, controles y acción principal |
| UI | Recursos propios, categorías, acción principal y foco | Selección con marca/texto; una acción principal; sin nuevas animaciones |

## Hallazgos corregidos

| Severidad | Dominio | Ubicación | Antes | Después | Motivo |
|---|---|---|---|---|---|
| HIGH | Colors | src/AiOPDF/MainWindow.cs:20 | Texto secundario #6F7F7B sobre blanco: 4.20:1 | #52665F: 6.13:1 sobre blanco y 5.83:1 sobre fondo del hero | AA 1.4.3 exige 4.5:1 para ese tamaño |
| HIGH | Accessibility | src/AiOPDF/MainWindow.cs:145 | Etiquetas visuales sin nombre programático en campos | AutomationProperties.SetName en Field, buscador y contraseña | Nombre accesible de controles |
| MEDIUM | Layout | src/AiOPDF/MainWindow.cs:139 | Dos columnas también en ventana estrecha | Una columna bajo 960 DIP | Evitar opciones estrechas y mejorar lectura |
| MEDIUM | Writing | src/AiOPDF/MainWindow.cs:106 | Búsqueda vacía sin salida explícita | Botón que limpia búsqueda y categoría | Recuperación del estado sin resultados |
| MEDIUM | UI | src/AiOPDF/MainWindow.cs:105 | Categoría activa indistinguible | Marca, etiqueta accesible y borde | Estado persistente que no depende solo de color |
| MEDIUM | Typography | src/AiOPDF/MainWindow.cs:74 | Interlineado implícito y alto fijo de tarjeta | Interlineado proporcional y MinHeight | Descripciones legibles y espacio para crecimiento |
| MEDIUM | Colors | src/AiOPDF/MainWindow.cs:125 | Seleccionar y guardar competían como acciones rellenas | Seleccionar secundario, guardar principal | Jerarquía de acción |

## Verificación

`dotnet run --project tests/AiOPDF.Checks -c Release -- --ui`: pasó navegación por las 25 herramientas, miniaturas, nombre del buscador, reinicio de filtros, categoría seleccionada y foco de teclado en tarjeta. Capturas guardadas en docs/screenshots: inicio, búsqueda vacía, foco, 860×620, 640×540 y herramienta compacta. Se inspeccionaron las capturas de inicio, organización, foco y tamaño mínimo.

Cálculo WCAG por luminancia sRGB de los pares sólidos declarados: texto secundario/blanco 6.13:1; secundario/hero 5.83:1; borde de campo/blanco 3.60:1; blanco/acción principal 5.48:1. No se midieron todos los estados internos de Fluent ni texto placeholder.

No verificado: paseo completo con lector de pantalla real, anuncios de progreso/error, escala de sistema al 200%, RTL, 320 DIP, Mac/Linux ni todas las combinaciones de hover/error/carga. La ventana soporta mínimo 640×540 DIP. No se afirma cumplimiento completo de WCAG.

## Veredicto

Approve para las correcciones y estados inspeccionados. La accesibilidad completa y los escenarios señalados como no verificados quedan pendientes; este informe no certifica esos escenarios.

## Revisión 0.3.0 con frontend-design
Se aplicó la dirección documentada en DESIGN.md: zona principal verde profundo, fuente Lato regular y bold incorporada, iconos sobre fondos por categoría, retirada de etiquetas en mayúsculas y flechas repetidas. La carga de archivos se compacta al seleccionar documentos. Se conservaron foco visible y adaptación por ancho. Capturas inspeccionadas: home.png y organize.png. Pruebas de filtros, teclado y 25 flujos completos pasaron; alcance y límites en VALIDATION.md. Lector de pantalla, anuncios y escala de sistema al 200% siguen sin verificar.

## 0.4 — controles propios y navegación

Ilustración vectorial DeskArt, plantillas StudioTheme, categorías laterales, búsqueda persistente, documentos compatibles reutilizados y opciones secundarias desplegables. Pruebas de ratón y teclado completadas; 25 guardados y 43 comprobaciones del motor pasan. Investigación en UX-RESEARCH.md. No se afirma certificación WCAG.
