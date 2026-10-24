namespace FIGO.backend;

public enum TipoCadastroFinanceiro { OrigemReceita, FormaPagamento, CategoriaDespesa }

public sealed record OpcaoFinanceira(int Id, string Nome);

public sealed class Pagador
{
    public int Id { get; set; }
    public string Tipo { get; set; } = "PF";
    public string Nome { get; set; } = "";
    public string? Documento { get; set; }
}

public sealed class ContaFinanceira
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Tipo { get; set; } = "Bancaria";
    public string Banco { get; set; } = "";
    public string Agencia { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Titular { get; set; } = "";
    public string Identificacao => Tipo == "Caixa" ? Nome + " • Caixa" : $"{Nome} • {Banco} • Ag. {Agencia} / Conta {Numero}";
}
