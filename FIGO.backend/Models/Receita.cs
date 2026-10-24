using System;

namespace FIGO.backend
{
    public class Receita
    {
        public int Id { get; set; }
        public int ViagemId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public int? OrigemId { get; set; }
        public int? PagadorId { get; set; }
        public int? FormaPagamentoId { get; set; }
        public int? ContaFinanceiraId { get; set; }
        public string PagadorNome { get; set; } = "";
        public string FormaPagamentoNome { get; set; } = "";
        public string ContaDestino { get; set; } = "";
        public string ReferenciaPagamento { get; set; } = "";
        public string Origem { get; set; } = string.Empty;
    }
}
