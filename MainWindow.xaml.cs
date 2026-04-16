using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WinForms = System.Windows.Forms;
using YoutubeMp3.Helpers;
using YoutubeMp3.Services;

namespace YoutubeMp3
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _cts;
        private string _downloadFolder = string.Empty;
        private bool _isBusy;

        // YouTube URL regex
        private static readonly Regex YoutubeRegex = new(
            @"^(https?://)?(www\.)?(youtube\.com/(watch\?.*v=|shorts/|embed/)|youtu\.be/)[A-Za-z0-9_\-]{11}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public MainWindow()
        {
            InitializeComponent();
            _downloadFolder = SettingsManager.GetDownloadFolder();
            UpdateFolderDisplay();

            Loaded += (_, _) => PlayStartupAnimation();
        }

        // ═══════════════ BAŞLANGIÇ ANİMASYONU ═══════════════
        private void PlayStartupAnimation()
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        // ═══════════════ URL PLACEHOLDER ═══════════════
        private void UrlTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UrlPlaceholder.Visibility = string.IsNullOrEmpty(UrlTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ═══════════════ KLASÖR SEÇ ═══════════════
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog
            {
                Description         = "MP3 dosyalarının kaydedileceği klasörü seçin",
                SelectedPath        = _downloadFolder,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                _downloadFolder = dialog.SelectedPath;
                SettingsManager.SaveDownloadFolder(_downloadFolder);
                UpdateFolderDisplay();
            }
        }

        private void UpdateFolderDisplay()
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            FolderPathText.Text = _downloadFolder == desktop
                ? "📁  Masaüstü"
                : $"📁  {_downloadFolder}";
        }

        // ═══════════════ İNDİR ═══════════════
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isBusy)
            {
                _cts?.Cancel();
                return;
            }

            var url = UrlTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                ShowStatus("warning", "⚠️", "Lütfen bir YouTube linki yapıştırın.");
                ShakeInput();
                return;
            }

            if (!YoutubeRegex.IsMatch(url))
            {
                ShowStatus("error", "❌", "Bu bir geçerli YouTube linki değil.");
                ShakeInput();
                return;
            }

            SetBusy(true);
            HideStatus();
            ResetProgress();
            _cts = new CancellationTokenSource();

            try
            {
                // 1) Araçları kontrol / indir
                var toolProgress = new Progress<(int percent, string message)>(p =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value    = p.percent;
                        ProgressLabel.Text   = p.message;
                        ProgressPercent.Text = $"%{p.percent}";
                    });
                });
                await DependencyChecker.EnsureToolsAsync(toolProgress, _cts.Token);

                // 2) İndir
                var dlProgress = new Progress<DownloadProgress>(p =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value    = p.Percent;
                        ProgressLabel.Text   = p.Message;
                        ProgressPercent.Text = p.Percent > 0 ? $"%{p.Percent}" : "";
                    });
                });
                await DownloadService.DownloadAsync(url, _downloadFolder, dlProgress, _cts.Token);

                // Başarı
                ShowStatus("success", "✅", $"Müzik kaydedildi → {_downloadFolder}");
                UrlTextBox.Clear();
                AnimateSuccess();
            }
            catch (OperationCanceledException)
            {
                ShowStatus("warning", "⏹", "İndirme iptal edildi.");
                ResetProgress();
            }
            catch (Exception ex)
            {
                ShowStatus("error", "❌", ex.Message);
                ResetProgress();
            }
            finally
            {
                SetBusy(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        // ═══════════════ UI YARDIMCILARI ═══════════════
        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            DownloadButton.Content = busy ? "⏹  İptal Et" : "🎵  Müziği İndir";
            UrlTextBox.IsEnabled   = !busy;
            BrowseButton.IsEnabled = !busy;
        }

        private void ResetProgress()
        {
            ProgressBar.Value    = 0;
            ProgressLabel.Text   = "Hazır";
            ProgressPercent.Text = "";
        }

        private void ShowStatus(string type, string icon, string message)
        {
            StatusIcon.Text = icon;
            StatusText.Text = message;
            StatusBorder.Visibility = Visibility.Visible;

            var (bg, fg, border) = type switch
            {
                "success" => ("#0F2A1A", "#22C55E", "#166534"),
                "error"   => ("#2A0F0F", "#F87171", "#991B1B"),
                _         => ("#2A200A", "#FBBF24", "#92400E"),
            };

            StatusBorder.Background  = new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(bg));
            StatusBorder.BorderBrush = new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(border));
            StatusBorder.BorderThickness = new Thickness(1.5);

            var fgBrush = new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(fg));
            StatusText.Foreground = fgBrush;
            StatusIcon.Foreground = fgBrush;

            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            StatusBorder.BeginAnimation(OpacityProperty, anim);
        }

        private void HideStatus()
        {
            StatusBorder.Visibility = Visibility.Collapsed;
        }

        private void ShakeInput()
        {
            var shake = new DoubleAnimationUsingKeyFrames();
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(8,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(6,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(0,  KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))));

            var transform = new TranslateTransform();
            UrlTextBox.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, shake);
        }

        private void AnimateSuccess()
        {
            ProgressBar.Foreground = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(34, 197, 94));

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                ResetProgress();
                ProgressBar.Foreground = (System.Windows.Media.Brush)FindResource("AccentSecondary");
            };
            timer.Start();
        }
    }
}