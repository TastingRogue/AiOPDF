# Comprobación de botones — 0.4.2

Windows, 12 de octubre de 2026. Se combinan pruebas de los manejadores reales de los controles con interacción nativa mediante la skill computer-use. Activar un manejador automáticamente no equivale a un clic físico en cada instancia.

| Botón/control | Evidencia |
|---|---|
| 25 tarjetas de herramientas y Volver | Activadas desde los controles; verificación de pantalla y regreso. |
| Categorías laterales | Todas activadas; recuento correspondiente comprobado. |
| Limpiar búsqueda y filtros | Restablece búsqueda; prueba existente de estado vacío. |
| Seleccionar/Añadir/Cambiar archivo | Selector Windows cargó ambos PDF de prueba; cancelar conserva el documento. Mismo manejador AddFiles para las tres etiquetas. |
| Quitar | Eliminación de selección y último archivo; guardar queda deshabilitado sin archivos. |
| Subir/Bajar | Orden de archivos comprobado en ambos sentidos. |
| Anterior/Siguiente | Documento de ocho páginas; cambio de grupos 1–6 y 7–8. |
| Actualizar vista previa | Conserva orden y selección; regresión corregida, incluida selección vacía. |
| Mover página antes/después | Cambia y restaura el orden indicado. |
| Incluir página | Space modifica selección; sincronización con campo de páginas comprobada. |
| Giro 90/180/270 | Cada botón produce un PDF con la rotación correspondiente. |
| Aumentar/Disminuir | Comprobados en todos los campos numéricos visibles de las herramientas. |
| Desplegables | Contraseña mediante Space; calidad expandida para comprobar controles. |
| Guardar resultado | Diálogo Windows produjo botones-unir-validado.pdf. Además, 25 herramientas ejecutadas con cada uno de los dos PDF. |
| Guardar archivos | Selector Windows produjo una carpeta AiOPDF-Split con tres PDF. |
| Guardar en encabezado compacto | Sesión nativa de 860 px; abre el selector de carpeta y permite cancelarlo. |
| Cancelar procesamiento | Pruebas de interfaz: sin archivo de resultado ni temporales pendientes. |
| Cancelar diálogo | Selector de archivos y carpeta cerrados sin sustituir entrada ni crear resultado adicional. |
| Abrir carpeta del resultado | Abrió la ventana pdf de File Explorer. |

El error encontrado estaba en Actualizar vista previa: reconstruía la selección con todas las páginas. La actualización conserva la selección existente y el orden, y se reinicia la cuenta al cargar un documento nuevo.

Comandos reproducibles: `--ui-preview` comprueba botones/navegación; `--ui` añade 50 guardados; `--native-dialogs` abre una sesión compacta para revisar el diálogo del encabezado; `--native-results` verifica los resultados conservados de las pruebas nativas. Todos se ejecutan con el proyecto tests/AiOPDF.Checks.

Los cuadros del sistema usan sus controles nativos. No se afirma un clic físico en cada botón repetido ni validación de interacción en Mac/Linux. Arrastrar y soltar no forma parte de esta comprobación de botones.

La prueba de selección vacía también detectó un evento TextChanged diferido que podía volver a incluir todas las páginas. Se distingue ahora el texto sincronizado desde las casillas del texto escrito por el usuario. Se prueba expresamente desmarcar todas y actualizar, así como ambos sentidos de movimiento y las tres rotaciones con resultados reales.

### 0.5.0
Comparar contenido solo está habilitado con dos archivos y muestra resultado sin crear archivos. Guardar informe exporta TXT/JSON después de revisar. Intercambiar versiones invalida el informe; seleccionar una fila no lo invalida. Los botones principales de otras herramientas nombran su operación. Campos obligatorios condicionan el botón de Eliminar páginas, Proteger PDF y Marca de agua.

