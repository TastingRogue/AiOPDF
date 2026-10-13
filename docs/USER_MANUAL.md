# User guide / Manual de uso

[English](USER_MANUAL.md) · [Español](USER_MANUAL.es.md) · [Repository](../README.md)

## Install and choose your language

Download the Windows x64 installer from the [candidate release](https://github.com/TastingRogue/AiOPDF/releases/tag/v1.0.0-rc.3). Run it, then open AiOPDF. No account or .NET SDK is needed. This candidate is unsigned; its release includes a SHA-256 manifest.

Choose **Español** or **English** in the header. The choice is saved locally and does not change your document content. For the Apple Silicon DMG and Ubuntu amd64 DEB, see the [installer guide](INSTALLERS.md), including tested targets and remaining signing/acceptance requirements.

## Process a document

1. Find a tool using search or the category menu.
2. Select local files or drop them into the workspace.
3. Review previews and set the options. For encrypted input, expand the password section.
4. Choose the action named for that tool, such as **Merge**, **Split** or **Add watermark**.
5. Review the prepared result, then choose **Save copy** and select a new name/location.

Preparing a result does not save it to your chosen destination yet. **Open result folder** becomes available after saving. Original files are preserved; existing output names are rejected.

You can use a reviewed PDF in another tool. This uses the prepared copy and does not replace the original.

## Pages and previews

Leave the page field empty for all pages, where the tool permits it. Use `1,3,5-8` for selected pages. Remove pages requires an explicit selection and at least one remaining page.

Organize preserves the specified order, such as `3,1,2`, and allows duplicates. Split supports one PDF per page, one per range or every N pages. Preview navigation is shown only when there are more pages beyond the current group.

## Compare two versions

Add exactly two PDFs: the original first, then the updated version. Use **Swap versions** if their order is wrong, then **Compare content**.

The result appears in the app. Browse full pages or jump between text changes. Pink underlines identify removed words; green identifies added words. **Save report** is optional and exports TXT/JSON.

Comparison is by page number. It does not compare images, meaning or align moved pages. Pages without extractable text require OCR before comparison.

## Choose the right tool

| You want to… | Use | Keep in mind |
| --- | --- | --- |
| Reduce size | Compress PDF | Lossless optimization may not reduce every file. |
| Hide page margins | Crop PDF | Hidden content is not removed. |
| Remove sensitive content in a region | Redact region | Selected pages are rasterized; review before sharing. |
| Search scanned pages | OCR to PDF | Latin-alphabet models; manually check recognized text. |
| Export editable paragraphs | Text to Word | Original layout is not preserved. |
| Read lines in a spreadsheet | Text to Excel | Tables and formulas are not reconstructed. |
| Put pages into a presentation | Pages to slides | Each slide contains an image, not editable text. |
| Open password-protected input | Remove password | You must know the existing password. |

## Cancel and recover

Cancellation is checked between processing operations; a native render already running may take time to return. Wait for the cancellation status before closing. Canceled reviewed-copy saving keeps the prepared result available for another attempt.

Review temporaries are removed on normal close. After an interruption, abandoned sessions are cleaned on the next start; active sessions are preserved. Exported files remain at the destination you selected.

## Report a problem

Use the [issue forms](https://github.com/TastingRogue/AiOPDF/issues/new/choose). Include app version, system, tool, steps and expected result. Reproduce with synthetic files; do not upload personal documents or passwords. For a security flaw, use [private reporting](../.github/SECURITY.md).

Development history and validation details are available in the [documentation index](README.md).
