namespace FIGO.Desktop
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            using var login = new FormLogin();
            if (login.ShowDialog() == DialogResult.OK && login.Usuario is { } usuario)
                Application.Run(new FormPrincipal(usuario));
        }
    }
}
