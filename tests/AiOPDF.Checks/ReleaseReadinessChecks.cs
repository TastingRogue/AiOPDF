using AiOPDF;
static class ReleaseReadinessChecks
{
    sealed class CancelProgress(CancellationTokenSource owner) : IProgress<long> { public void Report(long value) => owner.Cancel(); }
    public static void Run()
    {
        var root = Path.Combine(Path.GetFullPath("artifacts"), "release-readiness-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS readiness " + label); }
        try { ProcessingLimits.Raster(100000, 100000); throw new Exception("Raster limit ignored"); } catch (ArgumentException) { Check(true, "oversized raster rejected before allocation"); }
        ProcessingLimits.Raster(2480, 3508); Check(true, "normal A4 scan accepted");
        using (var active = new PrivateReviewSession(Path.Combine(root, "private")))
        {
            File.WriteAllText(Path.Combine(active.DirectoryPath, "document.txt"), "private test");
            PrivateReviewSession.Recover(Path.Combine(root, "private")); Check(Directory.Exists(active.DirectoryPath), "recovery leaves active session intact");
            var orphan = Path.Combine(root, "private", "AiOPDF-review-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(orphan); File.WriteAllText(Path.Combine(orphan, "session.lease"), ""); File.WriteAllText(Path.Combine(orphan, "document.txt"), "abandoned");
            var unknown = Path.Combine(root, "private", "user-document"); Directory.CreateDirectory(unknown);
            PrivateReviewSession.Recover(Path.Combine(root, "private")); Check(!Directory.Exists(orphan) && Directory.Exists(unknown), "recovery removes only abandoned marked session");
        }
        Check(Directory.GetDirectories(Path.Combine(root, "private"), "AiOPDF-review-*").Length == 0, "normal close removes private results");
        var childInfo = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardInput = true };
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) childInfo.ArgumentList.Add(typeof(ReleaseReadinessChecks).Assembly.Location);
        childInfo.ArgumentList.Add("--readiness-child"); childInfo.ArgumentList.Add(Path.Combine(root, "private"));
        using (var child = System.Diagnostics.Process.Start(childInfo)!)
        {
            var ready = child.StandardOutput.ReadLineAsync(); if (!ready.Wait(TimeSpan.FromSeconds(15))) { child.Kill(); throw new TimeoutException("Child startup"); }
            var abandoned = ready.Result!; Check(Directory.Exists(abandoned), "child creates private session");
            child.Kill(); child.WaitForExit();
            PrivateReviewSession.Recover(Path.Combine(root, "private")); Check(!Directory.Exists(abandoned), "restart recovers results after real process termination");
        }
        var source = Path.Combine(root, "large.bin"); File.WriteAllBytes(source, new byte[8 * 1024 * 1024]);
        var destination = Path.Combine(root, "saved.bin"); ReviewedCopy.CopyAsync(source, destination, default).GetAwaiter().GetResult(); Check(new FileInfo(destination).Length == new FileInfo(source).Length, "large copy completes");
        try { ReviewedCopy.CopyAsync(source, destination, default).GetAwaiter().GetResult(); throw new Exception("overwrite accepted"); } catch (ArgumentException) { Check(true, "existing destination preserved"); }
        using var canceled = new CancellationTokenSource(); var partial = Path.Combine(root, "canceled.bin");
        try { ReviewedCopy.CopyAsync(source, partial, canceled.Token, new CancelProgress(canceled)).GetAwaiter().GetResult(); throw new Exception("cancel ignored"); } catch (OperationCanceledException) { Check(!File.Exists(partial) && Directory.GetFileSystemEntries(root, ".aiopdf-reviewed-*").Length == 0, "cancel leaves neither final nor staging output"); }
        try { ReviewedCopy.CopyAsync(source, Path.Combine(root, "missing", "out.bin"), default).GetAwaiter().GetResult(); throw new Exception("missing destination accepted"); } catch (ArgumentException) { Check(true, "missing destination produces recoverable error"); }
        Console.WriteLine("Evidence: " + root);
    }
}
