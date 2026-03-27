# ⚡ Power & Sleep Mode — PowerPlanController

> **Türkçe** | [English](#english)

Windows sistem tepsisinden pil/şarj modlarına göre **ekran kapanma** ve **uyku sürelerini** anında değiştiren, hafif ve taşınabilir bir tray uygulaması.

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)
![Language](https://img.shields.io/badge/language-C%23-blue)

---

## 📸 Önizleme

> Uygulama, sağ alt köşede sistem tepsisinin hemen üstünde açılır.

---

## ✨ Özellikler

| # | Özellik |
| - | ------- |
| 1 | **Bağımsız Mod Kontrolü** — Pilde ve Şarjda modları için ayrı ayrı ekran/uyku süreleri |
| 2 | **Çoklu Dil (i18n)** — İşletim sistemi diline (TR / EN) otomatik arayüz |
| 3 | **Windows 11 Fluent Tasarım** — Koyu tema, yuvarlak köşeler, Mica tadında saydamlık, fade animasyonu |
| 4 | **Sistem Tepsisine Küçültme** — Opsiyonel; pencereyi kapatınca tepside gizli çalışmaya devam eder |
| 5 | **Otomatik Başlangıç** — Registry yardımıyla Windows açılışında otomatik başlar |
| 6 | **Mevcut Ayarları Okuma** — Açılışta `powercfg /query` ile gerçek anlık değerleri çeker |
| 7 | **Hata Denetimi** — Uyku süresinin ekran süresinden küçük girilmesini engeller |

---

## 🛠️ Kullanılan Teknolojiler

- **Dil:** C# 12 / .NET 8.0
- **Arayüz:** Windows Forms (WinForms)
- **Sistem İletişimi:** `powercfg.exe` CLI Wrapper (Windows yerleşik aracı)
- **Veri Saklama:** `%AppData%\PowerPlanController\config.json`
- **Dağıtım:** Single-file, Self-Contained EXE (kurulum gerektirmez)

> **Hiçbir üçüncü taraf NuGet paketi kullanılmamıştır.**
>
> Yalnızca .NET 8 SDK'nın standart kütüphaneleri (`System.Text.Json`, `Microsoft.Win32`, `System.Drawing`, `System.Windows.Forms`) kullanılmaktadır.

---

## 🚀 Derleme & Çalıştırma

### Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (Build için)
- Windows 10 veya Windows 11 (Çalıştırmak için)

### Derleme

```bash
# Geliştirme
dotnet build

# Tek dosya EXE (Release)
dotnet publish -c Release -r win-x64 --self-contained false
```

Derlenen EXE `publish/` klasöründe oluşur.

### Doğrudan Çalıştırma

```bash
dotnet run
```

---

## 📂 Proje Yapısı

```text
PowerPlanController/
├── assets/
│   └── icon.ico          # Gömülü uygulama ikonu
├── I18n.cs               # Türkçe / İngilizce çeviri sabitleri
├── PowerManager.cs       # powercfg.exe sarmalayıcısı (okuma & yazma)
├── Program.cs            # Giriş noktası, tek örnek (mutex) koruması
├── SettingsForm.cs       # Ana UI formu (tray popup, animasyon, tema)
├── TrayApp.cs            # Sistem tepsisi simgesi ve yaşam döngüsü
├── app.manifest          # UAC "Administrator" gereksinimi
└── PowerPlanController.csproj
```

---

## 🔑 Yönetici Yetkisi

`app.manifest` dosyasındaki `requireAdministrator` ayarı sayesinde uygulama,
`powercfg /change` komutlarının çalışabilmesi için otomatik olarak yönetici yetkisiyle başlar.

---

## 📄 Lisans

MIT © [Legendnoobe](https://github.com/Legendnoobe)

---

---

## ⚡ Power & Sleep Mode — PowerPlanController {#english}

> [Türkçe](#-power--sleep-mode--powerplancontroller) | **English**

A lightweight, portable Windows system-tray application that lets you instantly change **screen-off** and **sleep timeouts** for battery and plugged-in modes — without digging through Control Panel.

---

## ✨ Features

| # | Feature |
| - | ------- |
| 1 | **Independent Mode Control** — Separate display/sleep timers for Battery and Plugged-in |
| 2 | **Automatic Localization (i18n)** — UI adapts to the OS language (TR / EN) automatically |
| 3 | **Windows 11 Fluent Design** — Dark theme, rounded corners, fade-in/out animation |
| 4 | **System Tray Minimize** — Optional; hides to tray instead of closing |
| 5 | **Start with Windows** — Registers itself in the Registry for auto-start |
| 6 | **Read Current Settings** — Reads real live values from `powercfg /query` on launch |
| 7 | **Validation** — Prevents sleep timeout from being shorter than the screen-off timeout |

---

## 🛠️ Technologies

- **Language:** C# 12 / .NET 8.0
- **UI:** Windows Forms (WinForms)
- **System Communication:** `powercfg.exe` CLI Wrapper (built-in Windows tool)
- **Storage:** `%AppData%\PowerPlanController\config.json`
- **Distribution:** Single-file, self-contained EXE (no installer needed)

> **No third-party NuGet packages are used.**
>
> Only standard .NET 8 SDK libraries (`System.Text.Json`, `Microsoft.Win32`, `System.Drawing`, `System.Windows.Forms`).

---

## 🚀 Build & Run

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (to build)
- Windows 10 or Windows 11 (to run)

### Build

```bash
# Development
dotnet build

# Single-file EXE (Release)
dotnet publish -c Release -r win-x64 --self-contained false
```

The compiled EXE will appear in the `publish/` directory.

### Run directly

```bash
dotnet run
```

---

## 📂 Project Structure

```text
PowerPlanController/
├── assets/
│   └── icon.ico          # Embedded application icon
├── I18n.cs               # Turkish / English translation constants
├── PowerManager.cs       # powercfg.exe wrapper (read & write)
├── Program.cs            # Entry point, single-instance (mutex) guard
├── SettingsForm.cs       # Main UI form (tray popup, animations, theme)
├── TrayApp.cs            # System tray icon and lifecycle
├── app.manifest          # UAC "Administrator" requirement
└── PowerPlanController.csproj
```

---

## 🔑 Administrator Privilege

The `app.manifest` `requireAdministrator` setting ensures the app starts with elevated rights
so that `powercfg /change` commands always succeed.

---

## 📄 License

MIT © [Legendnoobe](https://github.com/Legendnoobe)
