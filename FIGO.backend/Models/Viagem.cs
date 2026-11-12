using System;

namespace FIGO.backend
{
    public class Viagem
    {
        public const int MaximoParticipantes = 10_000;
        private string nome = string.Empty;
        private string destino = string.Empty;

        public int Id { get; set; }
        public string Nome
        {
            get => nome;
            set => nome = (value ?? string.Empty).Trim();
        }
        public string Destino
        {
            get => destino;
            set => destino = (value ?? string.Empty).Trim();
        }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int QuantidadeParticipantes { get; set; }

        // Usado na leitura pelo ADO.NET e em inicializadores de objetos.
        public Viagem() { }

        public Viagem(string nome, string destino, DateTime dataInicio,
            DateTime dataFim, int quantidadeParticipantes)
        {
            Nome = nome;
            Destino = destino;
            DataInicio = dataInicio.Date;
            DataFim = dataFim.Date;
            QuantidadeParticipantes = quantidadeParticipantes;
            Validar();
        }

        public void Validar()
        {
            if (Nome.Length is 0 or > 255)
                throw new ArgumentException("Informe um nome de viagem com até 255 caracteres.");
            if (Destino.Length is 0 or > 255)
                throw new ArgumentException("Informe um destino com até 255 caracteres.");
            if (DataInicio.Year < 1000 || DataFim.Year < 1000)
                throw new ArgumentException("Informe as datas de início e fim da viagem.");
            if (DataFim.Date < DataInicio.Date)
                throw new ArgumentException("A data fim não pode ser antes da data início.");
            // Zero continua permitido durante o planejamento da equipe.
            if (QuantidadeParticipantes < 0 || QuantidadeParticipantes > MaximoParticipantes)
                throw new ArgumentException($"Informe de 0 a {MaximoParticipantes} participantes.");
        }

        public int CalcularDuracaoEmDias(bool incluirUltimoDia = true)
        {
            Validar();
            return (DataFim.Date - DataInicio.Date).Days + (incluirUltimoDia ? 1 : 0);
        }

        // Sem Receitas/Despesas/Saldo aqui: esses valores vêm do
        // ResumoService, calculados a partir do que está no banco.
    }
}
