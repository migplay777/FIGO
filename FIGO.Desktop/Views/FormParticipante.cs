using FIGO.backend;

namespace FIGO.Desktop;

internal sealed class FormParticipante : FormCadastroFinanceiro
{
    private readonly CheckBox existente = new() { Text = "Usar pessoa já cadastrada", AutoSize = true };
    private readonly ComboBox pessoas = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Nome", ValueMember = "Id" };
    private readonly TextBox nome = new() { MaxLength = 150 };
    private readonly TextBox cpf = new() { MaxLength = 14 };
    private readonly TextBox idade = new() { MaxLength = 3 };
    private readonly ComboBox funcao = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Nome", ValueMember = "Id" };
    private readonly int viagemId;
    private readonly ParticipacaoViagem? original;

    // Listas recebidas prontas: o construtor não acessa banco nem altera o objeto original.
    public FormParticipante(int viagemId, IReadOnlyList<Participante> cadastrados,
        IReadOnlyList<OpcaoFinanceira> funcoes, ParticipacaoViagem? original = null)
        : base(original == null ? "Adicionar participante à viagem" : "Editar participante da viagem", 610)
    {
        this.viagemId = viagemId; this.original = original;
        var opcoes = new List<OpcaoFinanceira> { new(0, "Selecione uma pessoa") };
        opcoes.AddRange(cadastrados.Select(p => new OpcaoFinanceira(p.Id, $"{p.NomeCompleto} — {p.CpfMascarado} (#{p.Id})")));
        pessoas.DataSource = opcoes;
        funcao.DataSource = new[] { new OpcaoFinanceira(0, "Não informada") }.Concat(funcoes).ToList();
        Campo("Cadastro", existente); Campo("Pessoa existente", pessoas); Campo("Nome completo *", nome);
        Campo("Idade na viagem", idade); Campo("Função na viagem", funcao); Campo("CPF (opcional)", cpf);
        Nota("A lista é opcional. Para incluir alguém, informe o nome. Idade, função e CPF podem ficar em branco. Nome e CPF são compartilhados com as outras viagens dessa pessoa.");
        existente.CheckedChanged += (_, _) => AlternarCadastro();
        if (original != null)
        {
            nome.Text = original.NomeCompleto; cpf.Text = original.Cpf ?? ""; idade.Text = original.Idade?.ToString() ?? "";
            funcao.SelectedValue = original.FuncaoParticipanteId ?? 0;
        }
        AlternarCadastro();
    }
    private void AlternarCadastro()
    {
        existente.Enabled = original == null; pessoas.Enabled = original == null && existente.Checked;
        nome.Enabled = cpf.Enabled = original != null || !existente.Checked;
    }
    protected override Task<int> GravarAsync()
    {
        int? anos = null;
        if (!string.IsNullOrWhiteSpace(idade.Text))
        {
            if (!int.TryParse(idade.Text, out int numero)) throw new ArgumentException("Informe a idade em anos inteiros ou deixe em branco.");
            anos = numero;
        }
        int? funcaoId = (int?)funcao.SelectedValue;
        if (funcaoId == 0) funcaoId = null;
        var service = new ParticipantesService();
        if (original != null)
        {
            var alterada = new ParticipacaoViagem { Id = original.Id, ViagemId = viagemId, ParticipanteId = original.ParticipanteId,
                NomeCompleto = nome.Text, Cpf = cpf.Text, Idade = anos, FuncaoParticipanteId = funcaoId };
            return Task.Run(() => { service.Editar(alterada); return alterada.Id; });
        }
        var pessoa = new Participante { NomeCompleto = nome.Text, Cpf = cpf.Text };
        if (existente.Checked)
        {
            pessoa.Id = (int?)pessoas.SelectedValue ?? 0;
            if (pessoa.Id == 0) throw new ArgumentException("Selecione a pessoa já cadastrada.");
        }
        return Task.Run(() => service.Adicionar(viagemId, pessoa, anos, funcaoId));
    }
}

internal sealed class FormNovaFuncaoParticipante : FormCadastroFinanceiro
{
    private readonly TextBox nome = new() { MaxLength = 100 };
    public FormNovaFuncaoParticipante() : base("Nova função de participante", 230)
    {
        Campo("Nome da função *", nome); Nota("A função ficará disponível para seleção nas viagens. Exemplos: cozinheiro, tesoureiro, voluntário.");
    }
    protected override Task<int> GravarAsync()
    {
        string texto = nome.Text;
        return Task.Run(() => new ParticipantesService().AdicionarFuncao(texto));
    }
}
