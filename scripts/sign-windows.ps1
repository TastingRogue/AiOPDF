param([Parameter(Mandatory=$true)][string]$Path, [Parameter(Mandatory=$true)][string]$CertificateThumbprint)
$ErrorActionPreference = 'Stop'
$certificate = Get-Item -LiteralPath ('Cert:\CurrentUser\My\' + $CertificateThumbprint)
if (!$certificate.HasPrivateKey) { throw 'El certificado debe disponer de clave privada.' }
$signature = Set-AuthenticodeSignature -LiteralPath $Path -Certificate $certificate -HashAlgorithm SHA256 -TimestampServer 'http://timestamp.digicert.com'
if ($signature.Status -ne 'Valid') { throw ('Firma inválida: ' + $signature.StatusMessage) }
