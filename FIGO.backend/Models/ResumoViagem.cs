namespace FIGO.backend
{
    public class ResumoViagem
    {
        public int ViagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public int QuantidadeParticipantes { get; set; }
        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal Saldo { get; set; }
    }
}
