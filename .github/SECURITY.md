# Security policy

AiOPDF processes local documents that may contain sensitive information. Security reports should stay private until maintainers have assessed them.

## Supported code

Security fixes target the current development branch and latest release candidate. The project is approaching stable 1.0; it has no guaranteed response time or long-term-support schedule. Older candidate builds should be replaced when a fix is available.

## Report privately

Use [GitHub private vulnerability reporting](https://github.com/TastingRogue/AiOPDF/security/advisories/new). Do not include exploit details, sensitive PDFs, passwords or certificates in public issues or pull requests.

Include the affected app version, operating system, tool, reproduction steps, impact and a minimal synthetic proof of concept when possible. If private reporting is unavailable, contact the maintainer through [TastingRogue's profile](https://github.com/TastingRogue) to arrange a private channel; do not post sensitive details on the profile publicly.

Reports are reviewed on a best-effort basis. Disclosure and attribution can be coordinated with the reporter after a fix is available.

## Scope and limits

This policy covers AiOPDF code, its packaging, language modules and integration with native libraries. Dependency vulnerabilities may also require an upstream fix.

Document processing is local, but a malformed PDF can still exercise native parsers. Resource limits are not a sandbox against every malicious document. Crop hides content; redaction rasterizes selected pages. Review output before sharing it. Current Windows candidates are unsigned, and macOS/Linux native release acceptance remains incomplete.
