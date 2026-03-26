using System.Globalization;

namespace PowerPlanController;

public static class I18n
{
    public static bool IsTr => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase) || 
                               CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase);

    public static string AppName   => IsTr ? "Güç & Uyku Modu" : "Power & Sleep Mode";
    public static string OnBattery => IsTr ? "PİLDE" : "ON BATTERY";
    public static string PluggedIn => IsTr ? "ŞARJDA" : "PLUGGED IN";
    public static string ScreenOff => IsTr ? "Ekranı kapat" : "Turn off display";
    public static string Sleep     => IsTr ? "Uyut" : "Sleep";
    public static string Never     => IsTr ? "Hiçbir zaman" : "Never";
    
    public static string LoadMin(int m) => m == 0 ? Never : string.Format(IsTr ? "{0} dk" : "{0} min", m);

    public static string TrayToggle => IsTr ? "Sistem tepsisinde göster" : "Show in system tray";
    public static string Startup    => IsTr ? "Sistem başlangıcında çalıştır" : "Start with Windows";
    public static string Apply      => IsTr ? "Uygula" : "Apply";
    public static string Applied    => IsTr ? "Ayarlar uygulandı ✓" : "Settings applied ✓";
    public static string Loaded     => IsTr ? "Ayarlar yüklendi ✓" : "Settings loaded ✓";
    
    public static string WarnSize(string mode) => IsTr 
        ? $"{mode}: Uyku süresi ekran kapatma süresinden küçük olamaz." 
        : $"{mode}: Sleep time cannot be less than display turn off time.";
        
    public static string Settings => IsTr ? "Ayarlar" : "Settings";
    public static string Exit     => IsTr ? "Çıkış" : "Exit";
    
    public static string ModeBattery => IsTr ? "Pilde" : "On Battery";
    public static string ModePlugged => IsTr ? "Şarjda" : "Plugged in";
    public static string Warning     => IsTr ? "Uyarı" : "Warning";
}
