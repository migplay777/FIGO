using System;

namespace FIGO.backend
{
    public class Despesa
    {
        public int Id { get; set; }
        public int ViagemId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public int? CategoriaId { get; set; }
        public string Categoria { get; set; } = string.Empty;
    }
}
