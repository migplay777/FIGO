namespace FIGO.backend;

public sealed record LinhaOrcamento(int ItemId, int CategoriaId, string Categoria, decimal Previsto, decimal Realizado)
{
    public decimal Diferenca => Previsto - Realizado;
    public string Situacao => Realizado > Previsto ? "Acima do previsto" : "Dentro do previsto";
}
public sealed record ResumoMeta(int Id, string Descricao, decimal ValorMeta, DateTime? DataLimite, decimal Arrecadado)
{
    public decimal Falta => Math.Max(0, ValorMeta - Arrecadado);
    public decimal Percentual => ValorMeta > 0 ? decimal.Round(Arrecadado / ValorMeta * 100, 2) : 0;
    public string Situacao => Arrecadado >= ValorMeta ? "Concluída" : DataLimite?.Date < DateTime.Today ? "Prazo encerrado" : "Em andamento";
}
public sealed class ContaPagar
{
    public int Id { get; set; }
    public int ViagemId { get; set; }
    public string Descricao { get; set; } = "";
    public string Credor { get; set; } = "";
    public int CategoriaId { get; set; }
    public string Categoria { get; set; } = "";
    public decimal ValorTotal { get; set; }
    public DateTime Vencimento { get; set; } = DateTime.Today;
    public decimal Pago { get; set; }
    public decimal Saldo => ValorTotal - Pago;
    public string Situacao => Saldo <= 0 ? "Paga" : Vencimento.Date < DateTime.Today ? "Vencida" : Pago > 0 ? "Parcial" : "Aberta";
}
public sealed record PagamentoRegistrado(int Id, int ContaPagarId, int DespesaId, decimal Valor, DateTime Data, string Referencia);
public sealed record AnexoResumo(int Id, string NomeOriginal, string TipoArquivo, int TamanhoBytes, string Sha256);
public sealed record ArquivoAnexo(AnexoResumo Metadados, byte[] Conteudo);

public static class ValidacaoPlanejamento
{
    public static void Id(int id, string campo) { if (id <= 0) throw new ArgumentException("Selecione " + campo + "."); }
    public static void Valor(decimal valor)
    {
        if (valor <= 0 || valor > 999999999.99m || decimal.Round(valor, 2) != valor)
            throw new ArgumentException("Informe um valor positivo, até R$ 999.999.999,99, com duas casas decimais.");
    }
    public static void Data(DateTime data) { if (data.Year < 1900) throw new ArgumentException("Informe uma data a partir de 1900."); }
    public static void Conta(ContaPagar c)
    {
        Id(c.ViagemId, "uma viagem"); Id(c.CategoriaId, "uma categoria");
        c.Descricao = ValidacaoFinanceira.Nome(c.Descricao, "Descrição", 255);
        c.Credor = ValidacaoFinanceira.Nome(c.Credor, "Credor", 150); Valor(c.ValorTotal); Data(c.Vencimento);
    }
}
