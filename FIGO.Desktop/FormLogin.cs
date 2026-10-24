using FIGO.backend;
using MySqlConnector;

namespace FIGO.Desktop;

public sealed class FormLogin : Form
{
    private readonly TextBox usuario = new() { MaxLength = 50 };
    private readonly TextBox senha = new() { UseSystemPasswordChar = true, MaxLength = 128 };
    private readonly TextBox confirmacao = new() { UseSystemPasswordChar = true, MaxLength = 128 };
    private readonly Label confirmarLabel = new();
    private readonly Label mensagem = new();
    private readonly Button enviar = UiTheme.PrimaryButton("Entrar");
    private readonly LinkLabel alternar = new();
    private readonly CheckBox mostrar = new();
    private readonly UsuarioService service = new();
    private bool cadastro;
    private int falhas;
    private DateTime tentarApos;
    public UsuarioAutenticado? Usuario { get; private set; }

    public FormLogin()
    {
        Text = "FIGO • Acesso";
        ClientSize = new Size(460, 590);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UiTheme.Background;
        Font = UiTheme.Font(10);
        Controls.Add(new FigoLogo { Left = 40, Top = 20, Width = 75, Height = 80 });
        Controls.Add(new Label { Text = "FIGO", Left = 128, Top = 35, AutoSize = true, Font = UiTheme.Font(28, FontStyle.Bold), ForeColor = UiTheme.Navy });
        Controls.Add(new Label { Text = "Acesse suas viagens e finanças", Left = 40, Top = 115, AutoSize = true, ForeColor = UiTheme.Muted });
        AdicionarCampo("Usuário", usuario, 155);
        AdicionarCampo("Senha", senha, 230);
        confirmarLabel.Text = "Confirme a senha";
        confirmarLabel.SetBounds(40, 305, 370, 24);
        confirmacao.SetBounds(40, 332, 380, 30);
        Controls.Add(confirmarLabel); Controls.Add(confirmacao);
        mostrar.Text = "Mostrar senha"; mostrar.SetBounds(40, 375, 200, 25);
        mostrar.CheckedChanged += (_, _) => senha.UseSystemPasswordChar = confirmacao.UseSystemPasswordChar = !mostrar.Checked;
        Controls.Add(mostrar);
        mensagem.SetBounds(40, 406, 380, 65); mensagem.ForeColor = UiTheme.Red;
        Controls.Add(mensagem);
        enviar.SetBounds(40, 480, 380, 40);
        enviar.Click += async (_, _) => await EnviarAsync(); Controls.Add(enviar);
        alternar.SetBounds(40, 536, 380, 28);
        alternar.LinkClicked += (_, _) => { cadastro = !cadastro; senha.Clear(); confirmacao.Clear(); mensagem.Text = ""; AtualizarModo(); };
        Controls.Add(alternar);
        AcceptButton = enviar;
        AtualizarModo();
    }
    private void AdicionarCampo(string titulo, Control campo, int y)
    {
        Controls.Add(new Label { Text = titulo, Left = 40, Top = y, Width = 380, Height = 24 });
        campo.SetBounds(40, y + 27, 380, 30); Controls.Add(campo);
    }
    private void AtualizarModo()
    {
        confirmarLabel.Visible = confirmacao.Visible = cadastro;
        enviar.Text = cadastro ? "Criar conta" : "Entrar";
        alternar.Text = cadastro ? "Já tenho conta — entrar" : "Criar uma conta";
        if (cadastro) mensagem.Text = "Use no mínimo 8 caracteres, com letra, número e sinal especial.";
    }
    private async Task EnviarAsync()
    {
        enviar.Enabled = alternar.Enabled = false;
        mensagem.Text = "";
        try
        {
            if (cadastro)
            {
                if (senha.Text != confirmacao.Text) throw new ArgumentException("As senhas não coincidem.");
                await service.CadastrarAsync(usuario.Text, senha.Text);
                cadastro = false; senha.Clear(); confirmacao.Clear(); AtualizarModo();
                mensagem.Text = "Conta criada. Digite sua senha para entrar.";
                senha.Focus();
            }
            else
            {
                if (DateTime.UtcNow < tentarApos) { mensagem.Text = "Aguarde 30 segundos antes de tentar novamente."; return; }
                var autenticado = await service.EntrarAsync(usuario.Text, senha.Text);
                if (autenticado == null)
                {
                    falhas++;
                    if (falhas >= 5) { tentarApos = DateTime.UtcNow.AddSeconds(30); falhas = 0; }
                    mensagem.Text = "Usuário ou senha incorretos."; senha.Clear(); senha.Focus(); return;
                }
                Usuario = autenticado;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (ArgumentException ex) { mensagem.Text = ex.Message; }
        catch (MySqlException ex) when (ex.Number == 1146) { mensagem.Text = "Banco ainda não preparado. Execute database/001_usuarios.sql."; }
        catch (MySqlException) { mensagem.Text = "Não foi possível acessar o MySQL. Verifique a conexão e tente novamente."; }
        catch (InvalidOperationException) { mensagem.Text = "Verifique a configuração FIGO_DB_CONNECTION e o cadastro no banco."; }
        catch (Exception) { mensagem.Text = "Não foi possível concluir. Tente novamente."; }
        finally { enviar.Enabled = alternar.Enabled = true; }
    }
}
