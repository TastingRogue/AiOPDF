# Idiomas de AiOPDF

Español e inglés se seleccionan en la cabecera. El cambio se aplica inmediatamente a la interfaz, sin reiniciar ni cambiar los archivos seleccionados, la herramienta, las contraseñas o los ajustes. El selector permanece visible en ventanas pequeñas mediante una segunda fila en la cabecera.

La elección se guarda localmente en `LocalApplicationData/AiOPDF/settings.json` (ruta específica del sistema), propiedad `Language`. El primer inicio usa español. Una configuración corrupta vuelve a español; un fallo al guardar la preferencia muestra un error y conserva el idioma anterior. No se necesita red.

## Estructura

- `src/AiOPDF/Localization/es.json`: catálogo de referencia y fallback.
- `src/AiOPDF/Localization/en.json`: traducción completa de las mismas claves.
- `L10n.cs`: descubre módulos, resuelve claves, valida parámetros y persiste la selección.
- `LocalizedUI.cs`: actualiza propiedades visibles y nombres accesibles con referencias débiles. No observa `TextBox.Text`, para conservar datos introducidos por la persona.

Los módulos incorporados se descubren automáticamente como recursos embebidos. También se descubren módulos adicionales en una carpeta `Localization` junto al ejecutable. Las opciones de procesamiento conservan sus valores internos originales; únicamente cambia su presentación. Las claves y estados del informe JSON permanecen estables; las instrucciones del informe TXT cambian con el idioma.

Los catálogos contienen textos de interfaz, instrucciones, errores propios, progreso y accesibilidad. Los errores provenientes del sistema operativo o bibliotecas externas conservan su texto original si no existe una traducción propia. La aplicación no traduce el contenido del documento ni las marcas de agua escritas por la persona. Cambiar el idioma de la interfaz tampoco cambia el alfabeto de los modelos OCR.

## Añadir un idioma

1. Copia `es.json` como `fr.json`, por ejemplo.
2. Cambia `code` a `fr`, `name` a `Français` y `culture` a `fr`. El nombre es el nombre nativo que aparecerá en el selector.
3. Traduce los valores de `messages`, conservando las claves. Conserva los parámetros `{0}`, `{1}`, etc.; puedes cambiar su posición en la frase. No cambies su cantidad ni sus identificadores.
4. Para distribuirlo con el código, añade el JSON a `src/AiOPDF/Localization/` y compila. El proyecto incorpora los archivos automáticamente.
5. Para probarlo sin recompilar, coloca el JSON en `Localization/` junto al ejecutable y vuelve a abrir la aplicación. El selector lo descubrirá al inicio.

Ejemplo de un módulo parcial válido:

```json
{
  "code": "fr",
  "name": "Français",
  "culture": "fr",
  "messages": {
    "common.language": "Langue",
    "common.files": "Fichiers",
    "ui.115": "Page {0}"
  }
}
```

Una clave ausente usa el texto español. Un módulo adicional con JSON inválido, cultura inválida o parámetros incompatibles se descarta; no sustituye los módulos integrados. Los idiomas con otros alfabetos pueden necesitar fuentes y ajustes de dirección del texto además de su catálogo. El alcance validado actualmente es español e inglés.

## Nuevos componentes

Usa claves estables con enlaces explícitos para nuevas propiedades de interfaz:

```csharp
var label = new TextBlock();
LocalizedUI.Bind(label, TextBlock.TextProperty, "ui.115", pageNumber);
// Si cambia pageNumber, vuelve a llamar Bind con el nuevo valor.
// El cambio de idioma actualiza el mismo control automáticamente.
```

Para un texto que se genera una sola vez, como un informe o el título de un diálogo del sistema, usa `L10n.Text(key)` o `L10n.Format(key, args)`. Escribe frases completas con parámetros. Nunca concatenes fragmentos traducidos alrededor de un número o nombre. Las formas plurales deben tener entradas diferenciadas cuando el idioma lo requiera.

La interfaz existente conserva un adaptador de compatibilidad que relaciona las cadenas españolas con las claves de referencia, incluidos mensajes dinámicos. No cambies esas cadenas como parte de una traducción; edita el catálogo. Para rediseñar un texto o añadir una vista, migra la propiedad a una clave explícita. Los fragmentos de documentos y vistas previas de TXT están excluidos de este adaptador.

## Validación

```powershell
& ./.tools/dotnet/dotnet.exe run --project tests/AiOPDF.Checks -c Release -- --localization
& ./.tools/dotnet/dotnet.exe run --project tests/AiOPDF.Checks -c Release -- --improvements-en
```

La primera comprueba cobertura de claves, parámetros, descubrimiento de un módulo externo, rechazo de parámetros incorrectos, fallback, selección, persistencia, búsqueda, mensajes dinámicos, accesibilidad, preservación de datos y las 25 vistas en ambos idiomas a 640 y 1440 px. La segunda procesa y guarda resultados reales de las herramientas en inglés y verifica comparación, geometría y conservación de originales. También se ejecutaron las regresiones existentes de interfaz y guardado en español. Los logs se conservan en `artifacts/localization-*.log`; las capturas bilingües se guardan en los directorios de evidencia de esas pruebas.

Validación local 2026-10-13, candidata 1.0.0-rc.2: 178 comprobaciones de localización, 129 de flujos en inglés, 485 regresiones de interfaz y 129 comprobaciones de flujos en español. Compilación final: cero advertencias y errores. Capturas: docs/screenshots/localization-home-en.png y localization-watermark-en-640.png. Las pruebas automatizadas usan Avalonia Headless en Windows; la validación nativa de Mac/Linux sigue pendiente.

Durante una primera ejecución del flujo inglés falló el guardado de la exportación JPG; no se conservó el detalle de la excepción. Se añadió diagnóstico de estado en los fallos de guardado de la prueba. Dos ejecuciones posteriores completas pasaron; el incidente queda pendiente de reproducción y no se atribuye a una causa corregida.


