using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeMp3.Helpers
{
    public static class DependencyChecker
    {
        private static readonly string ToolsDir = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "tools");

        public static string YtDlpPath  => System.IO.Path.Combine(ToolsDir, "yt-dlp.exe");
        public static string FfmpegPath => System.IO.Path.Combine(ToolsDir, "ffmpeg.exe");

        private const string YtDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        private const string FfmpegZipUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

        public static async Task EnsureToolsAsync(
            IProgress<(int percent, string message)> progress,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(ToolsDir);

            // yt-dlp indir
            if (!File.Exists(YtDlpPath))
            {
                progress.Report((0, "yt-dlp indiriliyor… (ilk kullanım)"));
                await DownloadFileAsync(YtDlpUrl, YtDlpPath,
                    p => progress.Report((p / 3, $"yt-dlp indiriliyor… %{p}")), ct);
                progress.Report((33, "yt-dlp hazır ✓"));
            }
            else
            {
                progress.Report((33, "yt-dlp hazır ✓"));
            }

            // ffmpeg indir (ZIP'ten extract)
            if (!File.Exists(FfmpegPath))
            {
                progress.Report((35, "ffmpeg indiriliyor… (ilk kullanım, biraz sürebilir)"));
                var zipPath = System.IO.Path.Combine(ToolsDir, "ffmpeg.zip");

                try
                {
                    await DownloadFileAsync(FfmpegZipUrl, zipPath,
                        p => progress.Report((33 + p * 50 / 100, $"ffmpeg indiriliyor… %{p}")), ct);

                    progress.Report((85, "ffmpeg çıkartılıyor…"));
                    await Task.Run(() => ExtractFfmpegFromZip(zipPath), ct);
                    progress.Report((95, "ffmpeg hazır ✓"));
                }
                finally
                {
                    // ZIP dosyasını temizle
                    try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                }
            }
            else
            {
                progress.Report((95, "ffmpeg hazır ✓"));
            }

            progress.Report((100, "Araçlar hazır ✓"));
        }

        private static void ExtractFfmpegFromZip(string zipPath)
        {
            using var archive = ZipFile.OpenRead(zipPath);

            // ffmpeg.exe'yi bul (iç klasörlerde olabilir)
            var ffmpegEntry = archive.Entries
                .FirstOrDefault(e => e.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase));

            if (ffmpegEntry != null)
            {
                ffmpegEntry.ExtractToFile(FfmpegPath, overwrite: true);
            }

            // ffprobe.exe de lazım olabilir
            var ffprobeEntry = archive.Entries
                .FirstOrDefault(e => e.Name.Equals("ffprobe.exe", StringComparison.OrdinalIgnoreCase));

            if (ffprobeEntry != null)
            {
                var ffprobePath = System.IO.Path.Combine(ToolsDir, "ffprobe.exe");
                ffprobeEntry.ExtractToFile(ffprobePath, overwrite: true);
            }
        }

        private static async Task DownloadFileAsync(
            string url,
            string destination,
            Action<int> onProgress,
            CancellationToken ct)
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 YoutubeMp3App/1.0");

            using var response = await http.GetAsync(url,
                HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? -1L;
            await using var src  = await response.Content.ReadAsStreamAsync(ct);
            await using var dest = new FileStream(destination,
                FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer     = new byte[81920];
            long downloaded = 0;
            int  read;
            int  lastReported = -1;

            while ((read = await src.ReadAsync(buffer, ct)) > 0)
            {
                await dest.WriteAsync(buffer.AsMemory(0, read), ct);
                downloaded += read;
                if (total > 0)
                {
                    var pct = (int)(downloaded * 100 / total);
                    if (pct != lastReported)
                    {
                        lastReported = pct;
                        onProgress(pct);
                    }
                }
            }
        }
    }
}
