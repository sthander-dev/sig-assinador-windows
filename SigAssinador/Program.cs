namespace SigAssinador;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var authorizationPath = args.FirstOrDefault(path =>
            path.EndsWith(".sigjob", StringComparison.OrdinalIgnoreCase) && File.Exists(path));
        Application.Run(new MainForm(authorizationPath));
    }
}
