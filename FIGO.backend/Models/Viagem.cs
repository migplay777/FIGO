using System;

namespace FIGO.backend
{
    public class Viagem
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int QuantidadeParticipantes { get; set; }

        // Sem Receitas/Despesas/Saldo aqui: esses valores vêm do
        // ResumoService, calculados a partir do que está no banco.
    }
}
