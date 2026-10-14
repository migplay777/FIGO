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
        public string Origem { get; set; } = string.Empty;
    }
}
