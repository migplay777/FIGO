namespace FIGO.backend
{
    // Novo. Cada linha representa uma fatia do gráfico
    // "Despesas por categoria" do Dashboard.
    public class DespesaCategoria
    {
        public string Categoria { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
