# Power & Sleep Mode (PowerPlanController)

Bu proje, Windows sistemlerinde **Ekranı Kapatma (Monitor Timeout)** ve **Uyku Modu (Standby Timeout)** özelliklerini hızlı ve pratik bir şekilde yönetmeyi sağlayan, Sistem Tepsisi (System Tray) tabanlı bir masaüstü uygulamasıdır.

## 📌 Projenin Amacı
Windows'un varsayılan Güç Planı ayarları genellikle klasörler içinde gömülü ve ulaşması zahmetlidir. Bu proje, kullanıcının sadece görev çubuğundaki (sağ alt köşe) tek bir ikona tıklayarak, pilde ve şarjda geçecek ekran kapanma ile uyku sürelerini tek bir arayüzden saniyeler içinde anında değiştirmesini sağlar.

## ✨ Temel Özellikler
1. **İki Mod İçin Bağımsız Kontrol:** Hem **Pilde** (On Battery) hem de **Şarjda** (Plugged In) modları için farklı süreler belirlenmesine olanak tanır.
2. **Çoklu Dil (i18n) Desteği:** İşletim sisteminin diline (Türkçe veya İngilizce) otomatik uyum sağlayarak kendi arayüzünü anında o dile çevirir.
3. **Windows 11 Fluent Tasarım:** Modern, yuvarlak hatlara sahip ve arka planda hafif transparan koyu renkler (Mica tadında) kullanan estetik bir açılır pencere (popup). Açılış ve kapanışlarda yavaşça belirip kaybolma (Fade) animasyonuna sahiptir.
4. **Sistem Tepsisine Küçültme:** Program penceresi kapatıldığında direkt kapanmaz, sistem tepsisine gizlenerek arkada çalışmaya devam edebilir (isteğe bağlı).
5. **Otomatik Başlangıç:** "Sistem başlangıcında çalıştır" (Start with Windows) kutucuğu işaretlendiğinde Registry yardımı ile her bilgisayar açılışında direkt görev çubuğuna yerleşir.
6. **Mevcut Ayarları Okuma:** Açılış anında Windows komut satırı aracı `powercfg` verilerini okuyup, bilgisayarın anlık geçerli ekran ve uyku ayarlarını GUI'ye (kullanıcı arayüzü) yansıtır.
7. **Hata Kontrolü:** Kullanıcının uyku süresini, ekran kapanma süresinden daha erkene kurmasına engel olan basit bir güvenlik mantığı (validation) mevcuttur.

## 🛠️ Kullanılan Teknolojiler & Mimari
- **Dil:** C# (.NET 8.0)
- **Arayüz (UI):** Windows Forms (WinForms)
- **Sistem İletişimi:** `powercfg` CLI Wrapper 
- **Veri Saklama:** Kullanıcı tercihleri ve tepsi ayarları json formatında `%AppData%\PowerPlanController\config.json` dizininde saklanmaktadır.
- **Mimari Format:** Self-Contained Single-File (Tam bağımsız tek dosya exe). Ekstra DLL dosya bağımlılığına veya kullanıcının sisteminde yüklü `.NET Runtime` arayışına gerek duymaz. Kurulumsuzdur (Portable).

## 🚀 Çalışma Şekli (Kısaca)
Uygulamanın içindeki `PowerManager.cs` sınıfı, arka planda gizlice Windows'un varsayılan `powercfg.exe` isimli aracını çalıştırır:
* Ekranı kapatma (Pilde) -> `powercfg /change monitor-timeout-dc`
* Uykuya geçme (Şarjda) -> `powercfg /change standby-timeout-ac`
gibi CLI kurallarına parametre gönderir. Form arayüzündeki Combobox verileri bu komutların argümanlarıdır.

Sonuç olarak `Output\PowerSleep.exe` tek dokunuşluk portatif bir "Güç & Uyku Modu" kontrolcüsüdür.
