namespace FIGO.backend;

public static class ValidacaoParticipantes
{
    public static void Pessoa(Participante pessoa)
    {
        pessoa.NomeCompleto = ValidacaoFinanceira.Nome(pessoa.NomeCompleto, "Nome completo", 150);
        var cpf = ValidacaoFinanceira.NormalizarDocumento(pessoa.Cpf ?? "");
        if (cpf.Length > 0 && !ValidacaoFinanceira.CpfValido(cpf))
            throw new ArgumentException("Informe um CPF válido ou deixe o campo em branco.");
        pessoa.Cpf = cpf.Length == 0 ? null : cpf;
    }
    public static void Vinculo(int viagemId, int? idade, int? funcaoId)
    {
        if (viagemId <= 0) throw new ArgumentException("Selecione uma viagem.");
        if (idade is < 0 or > 130) throw new ArgumentException("A idade deve estar entre 0 e 130 anos, ou ficar em branco.");
        if (funcaoId is <= 0) throw new ArgumentException("Selecione uma função válida ou deixe em branco.");
    }
}
