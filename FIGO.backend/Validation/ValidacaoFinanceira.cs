using System.Globalization;
using System.Text;

namespace FIGO.backend;

public static class ValidacaoFinanceira
{
    public static string Nome(string? valor, string campo, int tamanho = 100)
    {
        string nome = (valor ?? "").Trim();
        if (nome.Length == 0 || nome.Length > tamanho)
            throw new ArgumentException($"{campo}: informe de 1 a {tamanho} caracteres.");
        return nome;
    }

    public static string NormalizarDocumento(string? documento) =>
        new((documento ?? "").Trim().ToUpperInvariant().Where(c => c != '.' && c != '/' && c != '-' && !char.IsWhiteSpace(c)).ToArray());

    public static void Pagador(Pagador pagador)
    {
        pagador.Nome = Nome(pagador.Nome, "Nome / razão social", 150);
        string doc = NormalizarDocumento(pagador.Documento);
        if (pagador.Tipo == "PJ")
        {
            if (!CnpjValido(doc)) throw new ArgumentException("Informe um CNPJ válido para a empresa.");
        }
        else if (pagador.Tipo == "PF")
        {
            if (doc.Length > 0 && !CpfValido(doc)) throw new ArgumentException("O CPF é opcional; quando informado, deve ser válido.");
        }
        else throw new ArgumentException("Selecione pessoa física ou empresa.");
        pagador.Documento = doc.Length == 0 ? null : doc;
    }

    public static bool CpfValido(string documento)
    {
        string doc = NormalizarDocumento(documento);
        if (doc.Length != 11 || doc.Any(c => c < '0' || c > '9') || doc.Distinct().Count() == 1) return false;
        for (int n = 9; n <= 10; n++)
        {
            int soma = 0;
            for (int i = 0; i < n; i++) soma += (doc[i] - '0') * (n + 1 - i);
            int dv = (soma * 10) % 11;
            if (dv == 10) dv = 0;
            if (doc[n] - '0' != dv) return false;
        }
        return true;
    }

    // Receita Federal / Serpro: https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj/manual-dv-cnpj.pdf
    // Aceita CNPJ numérico e alfanumérico; os dois últimos caracteres são dígitos.
    public static bool CnpjValido(string documento)
    {
        string doc = NormalizarDocumento(documento);
        if (doc.Length != 14 || doc.Distinct().Count() == 1 ||
            doc.Take(12).Any(c => !(c >= '0' && c <= '9') && !(c >= 'A' && c <= 'Z')) ||
            doc.Skip(12).Any(c => c < '0' || c > '9')) return false;
        for (int n = 12; n <= 13; n++)
        {
            int soma = 0, peso = 2;
            for (int i = n - 1; i >= 0; i--) { soma += (doc[i] - 48) * peso; peso = peso == 9 ? 2 : peso + 1; }
            int resto = soma % 11;
            if (doc[n] - '0' != (resto < 2 ? 0 : 11 - resto)) return false;
        }
        return true;
    }

    public static void Conta(ContaFinanceira conta)
    {
        conta.Nome = Nome(conta.Nome, "Nome da conta");
        if (conta.Tipo == "Caixa") { conta.Banco = conta.Agencia = conta.Numero = conta.Titular = ""; return; }
        if (conta.Tipo != "Bancaria") throw new ArgumentException("Selecione o tipo da conta.");
        conta.Banco = Nome(conta.Banco, "Banco");
        conta.Agencia = Nome(conta.Agencia, "Agência", 20);
        conta.Numero = Nome(conta.Numero, "Número da conta", 30);
        conta.Titular = Nome(conta.Titular, "Titular", 150);
    }

    public static void Receita(Receita receita)
    {
        Lancamento(receita.ViagemId, receita.Descricao, receita.Valor, receita.Data);
        if (receita.OrigemId is not > 0 || receita.PagadorId is not > 0 ||
            receita.FormaPagamentoId is not > 0 || receita.ContaFinanceiraId is not > 0)
            throw new ArgumentException("Selecione a origem, o pagador, a forma de pagamento e a conta de destino.");
        if (receita.ReferenciaPagamento.Length > 100) throw new ArgumentException("A referência deve ter no máximo 100 caracteres.");
    }

    public static void Despesa(Despesa despesa)
    {
        Lancamento(despesa.ViagemId, despesa.Descricao, despesa.Valor, despesa.Data);
        if (despesa.CategoriaId is not > 0) throw new ArgumentException("Selecione uma categoria cadastrada para a despesa.");
    }

    private static void Lancamento(int viagemId, string descricao, decimal valor, DateTime data)
    {
        if (viagemId <= 0) throw new ArgumentException("Selecione uma viagem cadastrada.");
        Nome(descricao, "Descrição", 255);
        if (valor <= 0 || valor > 999999999.99m || decimal.Round(valor, 2) != valor)
            throw new ArgumentException("Informe um valor positivo com até duas casas decimais, no máximo R$ 999.999.999,99.");
        if (data.Year < 1900) throw new ArgumentException("Informe uma data válida a partir de 1900.");
    }
}

public static class BuscaFinanceira
{
    private static readonly CompareInfo Comparador = CultureInfo.GetCultureInfo("pt-BR").CompareInfo;
    public static bool Corresponde(string texto, string busca) => busca.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .All(termo => Comparador.IndexOf(texto, termo, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);
}
