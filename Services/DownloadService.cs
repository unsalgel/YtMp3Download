using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YoutubeMp3.Helpers;

namespace YoutubeMp3.Services
{
    public enum DownloadStage { FetchingInfo, Downloading, Converting, Done }

    public record DownloadProgress(DownloadStage Stage, int Percent, string Message);

    public static class DownloadService
    {
        // [download]  47.3% of 5.23MiB ...
        private static readonly Regex PercentRegex =
            new(@"\[download\]\s+([\d.]+)%", RegexOptions.Compiled);

        public static async Task DownloadAsync(
            string url,
            string outputFolder,
            IProgress<DownloadProgress> progress,
            CancellationToken ct = default)
        {
            var outputTemplate = System.IO.Path.Combine(outputFolder, "%(title)s.%(ext)s");
            var ffmpegDir       = System.IO.Path.GetDirectoryName(DependencyChecker.FfmpegPath)!;
            var ytDlpPath      = DependencyChecker.YtDlpPath;

            var args = string.Join(" ",
                $"\"{url}\"",
                "--extract-audio",
                "--audio-format mp3",
                "--audio-quality 0",
                "--embed-thumbnail",
                "--add-metadata",
                "--convert-thumbnails jpg",
                $"--ffmpeg-location \"{ffmpegDir}\"",
                $"--output \"{outputTemplate}\"",
                "--newline",
                "--no-playlist",
                "--geo-bypass",
                "--ignore-errors"
            );

            var psi = new ProcessStartInfo
            {
                FileName               = ytDlpPath,
                Arguments              = args,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                WindowStyle            = ProcessWindowStyle.Hidden,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding  = System.Text.Encoding.UTF8,
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            var errorLines = new ConcurrentBag<string>();
            var tcs        = new TaskCompletionSource<int>();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                var line = e.Data.Trim();

                if (line.StartsWith("[info]") || line.StartsWith("[youtube]"))
                {
                    progress.Report(new(DownloadStage.FetchingInfo, 0, "Video bilgileri alınıyor…"));
                }
                else if (line.StartsWith("[ExtractAudio]") || line.StartsWith("[ffmpeg]"))
                {
                    progress.Report(new(DownloadStage.Converting, 95, "MP3'e dönüştürülüyor…"));
                }
                else if (line.StartsWith("[download]"))
                {
                    var m = PercentRegex.Match(line);
                    if (m.Success && double.TryParse(
                            m.Groups[1].Value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var pct))
                    {
                        var scaled = (int)(pct * 0.90);
                        progress.Report(new(DownloadStage.Downloading, scaled,
                            $"İndiriliyor… %{pct:0.0}"));
                    }
                    else if (line.Contains("Destination:"))
                    {
                        progress.Report(new(DownloadStage.Downloading, 5, "İndirme başladı…"));
                    }
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    errorLines.Add(e.Data);
            };

            process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);

            progress.Report(new(DownloadStage.FetchingInfo, 0, "Başlatılıyor…"));
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            ct.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch { /* ignored */ }
            });

            await tcs.Task;

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);

            if (process.ExitCode != 0)
            {
                var errText = string.Join("\n", errorLines);
                throw new InvalidOperationException(MapError(errText));
            }

            progress.Report(new(DownloadStage.Done, 100, "✅ Müzik başarıyla kaydedildi!"));
        }

        private static string MapError(string raw)
        {
            raw = raw.ToLowerInvariant();

            if (raw.Contains("no internet") || raw.Contains("unable to connect") ||
                raw.Contains("network") || raw.Contains("connection"))
                return "İnternet bağlantınızı kontrol edin.";

            if (raw.Contains("video unavailable") || raw.Contains("private video") ||
                raw.Contains("has been removed"))
                return "Video bulunamadı veya erişilemiyor.";

            if (raw.Contains("is not a valid url") || raw.Contains("unsupported url"))
                return "Bu bir geçerli YouTube linki değil.";

            if (raw.Contains("copyright") || raw.Contains("blocked"))
                return "Bu video telif hakkı nedeniyle indirilemiyor.";

            return "Bir hata oluştu. Lütfen linki kontrol edip tekrar deneyin.";
        }
    }
}
