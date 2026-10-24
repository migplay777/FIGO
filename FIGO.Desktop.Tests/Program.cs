using FIGO.Desktop;
using System.Reflection;

namespace FIGO.Desktop.Tests;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var seletor = new PesquisaComboBox();
        seletor.DefinirOpcoes([new(1, "Alimentação"), new(2, "Transporte"), new(3, "Empresa Exemplo", "12ABC34501DE35")]);
        Confere(seletor.IdSelecionado == null, "O controle selecionou um cadastro sem ação do usuário.");
        seletor.Escolher(1);
        Confere(seletor.IdSelecionado == 1, "A seleção por ID falhou.");
        seletor.Pesquisar("menta");
        Confere(seletor.Items.Count == 1 && seletor.IdSelecionado == null, "A busca não filtrou por trecho ou reteve uma seleção antiga.");
        seletor.Pesquisar("12abc");
        Confere(seletor.Items.Count == 1 && ((ItemSelecao)seletor.Items[0]).Id == 3, "Busca pelo CNPJ falhou.");
        seletor.SelectedIndex = 0;
        typeof(PesquisaComboBox).GetMethod("OnSelectionChangeCommitted", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(seletor, [EventArgs.Empty]);
        Confere(seletor.IdSelecionado == 3, "A seleção do resultado filtrado falhou.");
        seletor.Text = "Empresa ainda não cadastrada";
        Confere(seletor.IdSelecionado == null, "Texto livre foi aceito como cadastro selecionado.");
        seletor.Pesquisar("Inexistente");
        Confere(seletor.Items.Count == 0 && seletor.IdSelecionado == null, "Busca sem resultado manteve um ID indevido.");
        seletor.Escolher(2);
        Confere(seletor.IdSelecionado == 2 && seletor.Items.Count == 3, "Recarregar a lista perdeu a seleção por ID.");
        Console.WriteLine("Seletores WinForms: busca, ID selecionado e prevenção de texto livre passaram.");
    }
    private static void Confere(bool condicao, string mensagem) { if (!condicao) throw new Exception(mensagem); }
}
