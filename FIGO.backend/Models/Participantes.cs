namespace FIGO.backend;

public sealed class Participante
{
    public int Id { get; set; }
    public string NomeCompleto { get; set; } = "";
    public string? Cpf { get; set; }
    public string CpfMascarado => MascararCpf(Cpf);
    public static string MascararCpf(string? cpf) => string.IsNullOrEmpty(cpf) ? "Não informado" : "***.***.***-" + cpf[^2..];
}

public sealed class ParticipacaoViagem
{
    public int Id { get; set; }
    public int ViagemId { get; set; }
    public int ParticipanteId { get; set; }
    public string NomeCompleto { get; set; } = "";
    public string? Cpf { get; set; }
    public int? Idade { get; set; }
    public int? FuncaoParticipanteId { get; set; }
    public string? Funcao { get; set; }
    public string CpfMascarado => Participante.MascararCpf(Cpf);
}
