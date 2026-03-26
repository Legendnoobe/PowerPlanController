namespace PowerPlanController;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ApplicationConfiguration.Initialize();

        // Require single instance
        using var mutex = new System.Threading.Mutex(true, "PowerPlanController_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("PowerPlanController zaten çalışıyor.", "Bilgi",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var trayApp = new TrayApp();
        Application.Run();
    }
}