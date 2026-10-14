namespace FIGO.Desktop;
public class ConfiguracoesView : UserControl
{
    public ConfiguracoesView()
    {
        BackColor = UiTheme.Background;
        Controls.Add(ViewHelpers.Header("Configurações", "Preferências e informações do sistema FIGO."));
        var info = new Label { Dock = DockStyle.Fill, Padding = new Padding(24), Font = UiTheme.Font(11), ForeColor = UiTheme.Text,
            Text = "FIGO — Igreja Família da Fé\n\nBanco de dados: MySQL\nConexão configurada pela variável FIGO_DB_CONNECTION.\n\nO sistema não armazena sua senha do banco no repositório." };
        Controls.Add(info);
        info.BringToFront();
    }
}
