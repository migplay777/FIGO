using FIGO.Desktop;
using FIGO.backend;
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
        Confere(seletor.Items.Count == 1 && seletor.Items[0] is ItemSelecao { Id: 3 }, "Busca pelo CNPJ falhou.");
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
        var viagem = new Viagem("Missão de teste", "Campinas", DateTime.Today, DateTime.Today.AddDays(1), 150);
        using (var editor = new FormEditarViagem(viagem))
        {
            Confere(editor.Controls.OfType<NumericUpDown>().Single().Value == 150,
                "A edição não abriu com mais de 100 participantes.");
            editor.Controls.OfType<TextBox>().First().Text = "Alteração ainda não salva";
        }
        Confere(viagem.Nome == "Missão de teste" && viagem.QuantidadeParticipantes == 150,
            "Cancelar a edição alterou a referência original da viagem.");
        Console.WriteLine("Editor WinForms: 150 participantes e cancelamento sem alterar o objeto passaram.");
        var participacao = new ParticipacaoViagem { Id = 1, ViagemId = 1, ParticipanteId = 1, NomeCompleto = "Maria de Teste", Idade = null, Cpf = null };
        using (var editor = new FormParticipante(1, [], [], participacao))
        {
            var campos = editor.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TextBox>().ToList();
            Confere(campos.Single(x => x.AccessibleName == "Idade na viagem").Text == "", "Idade opcional virou zero.");
            Confere(campos.Single(x => x.AccessibleName == "CPF (opcional)").Text == "", "CPF opcional não ficou vazio.");
            campos.Single(x => x.AccessibleName == "Nome completo *").Text = "Edição cancelada";
        }
        Confere(participacao.NomeCompleto == "Maria de Teste" && participacao.Idade == null, "Cancelar participante alterou objeto original.");
        using (var novo = new FormParticipante(1, [], []))
        {
            var controles = novo.Controls.OfType<TableLayoutPanel>().Single();
            var opcao = controles.Controls.OfType<CheckBox>().Single(); opcao.Checked = true;
            Confere(!controles.Controls.OfType<TextBox>().Single(x => x.AccessibleName == "Nome completo *").Enabled,
                "Reutilizar pessoa permite sobrescrever identidade sem edição explícita.");
        }
        Console.WriteLine("Participantes WinForms: opcionais vazios, reutilização e cancelamento passaram.");
        var conta = new ContaPagar { Id=1,ViagemId=1,Descricao="Hotel",Credor="Hotel de teste",CategoriaId=1,ValorTotal=100,Pago=30,Vencimento=DateTime.Today };
        using(var form=new FormPagamentoConta(conta))
        {
            var dinheiro=form.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<NumericUpDown>().Single();
            Confere(dinheiro.Maximum==70 && dinheiro.Value==70,"Pagamento não respeitou o saldo restante.");
        }
        using(var form=new FormContaPagar(1,[new(1,"Hospedagem")],conta))
        {
            var campos=form.Controls.OfType<TableLayoutPanel>().Single();
            Confere(!campos.Controls.OfType<ComboBox>().Single().Enabled,"Categoria de conta com pagamentos não foi protegida.");
            campos.Controls.OfType<TextBox>().First().Text="Edição cancelada";
        }
        Confere(conta.Descricao=="Hotel" && conta.ValorTotal==100,"Cancelar edição alterou a conta original.");
        using(var form=new FormMetaArrecadacao(1))
            Confere(!form.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<DateTimePicker>().Single().Checked,"Prazo opcional da meta foi preenchido automaticamente.");
        Console.WriteLine("Planejamento WinForms: saldo para pagamento, categoria protegida, cancelamento e prazo opcional passaram.");
    }
    private static void Confere(bool condicao, string mensagem) { if (!condicao) throw new Exception(mensagem); }
}
