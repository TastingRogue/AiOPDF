namespace AiOPDF;
public static class ReviewedCopy
{
    public static async Task CopyAsync(string source, string destination, CancellationToken token, IProgress<long>? progress = null)
    {
        var target = Path.GetFullPath(destination); var parent = Path.GetDirectoryName(target)!;
        if (!Directory.Exists(parent) || File.Exists(target) || Directory.Exists(target)) throw new ArgumentException("Elige una carpeta existente y un nombre nuevo para conservar los originales.");
        var staging = Path.Combine(parent, ".aiopdf-reviewed-" + Guid.NewGuid().ToString("N"));
        var folder = Directory.Exists(source); long copied = 0;
        try
        {
            token.ThrowIfCancellationRequested(); if (folder) Directory.CreateDirectory(staging);
            foreach (var file in folder ? Directory.GetFiles(source) : new[] { source })
            {
                await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var output = new FileStream(folder ? Path.Combine(staging, Path.GetFileName(file)) : staging, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous);
                var buffer = new byte[1024 * 1024]; int read;
                while ((read = await input.ReadAsync(buffer, token).ConfigureAwait(false)) > 0) { await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false); copied += read; progress?.Report(copied); }
                await output.FlushAsync(token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            if (folder) Directory.Move(staging, target); else File.Move(staging, target);
        }
        finally { if (File.Exists(staging)) File.Delete(staging); if (Directory.Exists(staging)) Directory.Delete(staging, true); }
    }
}
