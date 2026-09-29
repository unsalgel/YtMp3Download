# YouTube MP3 İndirici (WPF Desktop App)

Modern, sade ve koyu temalı bir arayüze sahip; YouTube videolarını otomatik olarak en yüksek kalitede MP3 formatına dönüştürüp indiren açık kaynaklı bir Windows masaüstü uygulamasıdır.

Arka planda **yt-dlp** ve **FFmpeg** araçlarını kullanarak harici kurulum gerektirmeden çalışır.

---

## Özellikler

- **Modern ve Akıcı Koyu Tema (Dark Theme):** WPF tabanlı, özel gradyanlar, kart gölgeleri ve yumuşak geçiş animasyonları.
- **Otomatik Bağımlılık Yönetimi:** İlk çalıştırmada gereken `yt-dlp` ve `ffmpeg` bileşenlerini otomatik olarak `%LocalAppData%\YoutubeMp3\tools` dizinine indirir ve yapılandırır. Kullanıcının manuel kurulum yapmasına gerek kalmaz.
- **Yüksek Ses Kalitesi (VBR 0):** En yüksek ses kalitesinde (`--audio-quality 0`) kayıpsız çevrim.
- **Albüm Kapağı ve Meta Veri:** İndirilen MP3 dosyalarına video kapak resmini (thumbnail) ve şarkı/sanatçı meta verilerini otomatik gömer.
- **Özelleştirilebilir İndirme Konumu:** İndirme klasörünü kolayca seçebilir ve tercihinizi kaydedebilirsiniz (varsayılan: *Masaüstü*).
- **Anlık İlerleme Takibi:** Yüzdelik oran, indirme durumu ve durum bildirimleri.
- **İptal Desteği:** İndirme esnasında işlemi dilediğiniz zaman durdurabilme (`CancellationToken` entegrasyonu).
- **Hata ve Bağlantı Doğrulama:** Geçersiz YouTube URL'lerini anında yakalayan regex kontrolü ve görsel sarsıntı (shake) efekti.

---

## Teknolojiler ve Kütüphaneler

- **Platform:** .NET 8.0 (Windows Desktop - WPF)
- **Paketler:**
  - `Newtonsoft.Json` (v13.0.3) – Ayar ve konfigürasyon yönetimi
- **Çekirdek Motorlar:**
  - [yt-dlp](https://github.com/yt-dlp/yt-dlp) – Medya indirme aracı
  - [FFmpeg](https://ffmpeg.org/) – Ses ayrıştırma, format dönüştürme ve ID3 etiketleme

---

## Başlangıç ve Kurulum

### Gereksinimler
- **Windows 10 / 11 (64-bit)**
- **.NET 8.0 Desktop Runtime** (veya Visual Studio 2022 / .NET 8 SDK)

### Projeyi Çalıştırma

1. **Repoyu klonlayın:**
   ```bash
   git clone https://github.com/unsalgel/YtMp3Download.git
   cd YtMp3Download
   ```

2. **Projeyi derleyin ve çalıştırın:**
   ```bash
   dotnet build
   dotnet run
   ```
   *(veya `YoutubeMp3.csproj` dosyasını Visual Studio ile açıp F5 tuşuna basın)*

3. **İlk Kullanım Notu:**
   Uygulama ilk kez bir indirme işlemi yaptığında `yt-dlp` ve `ffmpeg` dosyalarını otomatik indirecektir. Bu işlem internet hızınıza bağlı olarak ilk sefere mahsus birkaç saniye sürebilir.

---

## Dosya Yapısı

```
YoutubeMp3/
├── App.xaml / App.xaml.cs            # Uygulama kaynakları ve tema stilleri
├── MainWindow.xaml / .cs             # Ana pencere tasarımı, animasyonlar ve kontroller
├── Services/
│   └── DownloadService.cs            # yt-dlp ve ffmpeg süreçlerini yöneten servis
├── Helpers/
│   ├── DependencyChecker.cs          # Araçların (yt-dlp, ffmpeg) indirilmesi ve kontrolü
│   └── SettingsManager.cs            # İndirme klasörü ayarlarının JSON olarak saklanması
├── app.manifest                      # Windows uyumluluk ve DPI farkındalık ayarları
└── YoutubeMp3.csproj                 # Proje bağımlılık ve derleme yapılandırması
```

---

## Lisans

Bu proje kişisel ve eğitim amaçlı geliştirilmiştir. `yt-dlp` ve `FFmpeg` araçları kendi açık kaynak lisanslarına tabidir.
