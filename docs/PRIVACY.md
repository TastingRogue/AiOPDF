# Privacy

AiOPDF is a local desktop application. Documents and passwords are processed on the user's computer. It has no document upload service, local web server, account registration, analytics or automatic update client.

Language preferences are stored locally. Processing may create private temporary files, which are cleaned through the application's session lifecycle and recovery mechanisms. Saving creates output at the location chosen by the user. Keep exported results private when they contain sensitive information.

Opening the result folder invokes the operating system file manager. A destination chosen on a network drive or in a cloud-synchronized directory follows that destination's own transfer policies. AiOPDF does not control the user's operating system, backup software or sync clients.

Builds and code signing use GitHub Actions, SignPath (when approved) and Apple notarization (when configured). These developer services receive source/build artifacts and synthetic test evidence; they do not receive the documents processed by app users. Provider privacy information: [GitHub](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement), [SignPath](https://about.signpath.io/privacy-policy), [Apple](https://www.apple.com/legal/privacy/).

If you report a problem publicly, use synthetic documents. Send security reports through GitHub private vulnerability reporting as described in [SECURITY.md](../.github/SECURITY.md).
