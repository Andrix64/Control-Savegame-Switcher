namespace ControlSaveManager;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Result<SaveManager> init = SaveManager.Initialize();

        if (!init.Success)
        {
            MessageBox.Show(
                init.Message,
                "Control Save Manager",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Application.Run(new MainForm(init.Value!));
    }
}
