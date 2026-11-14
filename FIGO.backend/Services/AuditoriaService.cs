using MySqlConnector;

namespace FIGO.backend;

public sealed record RegistroAuditoria(long Id, DateTime OcorridoEmUtc, string Acao, string? Usuario,
    string Entidade, long RegistroId, string Origem, string UsuarioBanco)
{
    public DateTime DataLocal => OcorridoEmUtc.ToLocalTime();
    public string Autor => Usuario ?? "Não identificado";
}
public sealed record DetalheAuditoria(string Campo, string? ValorAnterior, string? ValorNovo);
public sealed record PaginaAuditoria(IReadOnlyList<RegistroAuditoria> Registros, long Total);

public sealed class AuditoriaService
{
    public static IReadOnlyList<string> Entidades { get; } = Array.AsReadOnly(new[] {
        "Viagem", "Receita", "Despesa", "Usuarios", "Pagador", "OrigemReceita", "FormaPagamento",
        "ContaFinanceira", "CategoriaDespesa", "Participante", "FuncaoParticipante", "ViagemParticipante" });

    public PaginaAuditoria Listar(DateTime inicioUtc, DateTime fimUtc, string? entidade = null,
        int? tipo = null, string? autor = null, int pagina = 1, int tamanhoPagina = 50)
    {
        if (inicioUtc.Kind != DateTimeKind.Utc || fimUtc.Kind != DateTimeKind.Utc || fimUtc <= inicioUtc)
            throw new ArgumentException("Informe um período válido em UTC.");
        if (entidade != null && !Entidades.Contains(entidade)) throw new ArgumentException("Entidade inválida.");
        if (tipo is < 1 or > 3 || pagina < 1 || tamanhoPagina is < 1 or > 100)
            throw new ArgumentException("Filtro ou página inválida.");
        if ((autor?.Length ?? 0) > 50) throw new ArgumentException("O filtro de autor permite até 50 caracteres.");
        long deslocamento = (long)(pagina - 1) * tamanhoPagina;
        const string where = " WHERE l.OcorridoEm >= @inicio AND l.OcorridoEm < @fim AND (@entidade IS NULL OR l.Entidade = @entidade) AND (@tipo IS NULL OR l.TipoEventoAuditoriaId = @tipo) AND (@autor IS NULL OR LOCATE(@autor, l.NomeUsuario) > 0)";
        using var c = Conexao.Abrir();
        using var count = new MySqlCommand("SELECT COUNT(*) FROM LogAuditoria l" + where, c);
        Parametros(count); long total = Convert.ToInt64(count.ExecuteScalar());
        using var cmd = new MySqlCommand("""
            SELECT l.Id, l.OcorridoEm, t.Nome, l.NomeUsuario, l.Entidade, l.RegistroId, l.Origem, l.UsuarioBanco
            FROM LogAuditoria l JOIN TipoEventoAuditoria t ON t.Id = l.TipoEventoAuditoriaId
            """ + where + " ORDER BY l.OcorridoEm DESC, l.Id DESC LIMIT @limite OFFSET @deslocamento", c);
        Parametros(cmd); cmd.Parameters.AddWithValue("@limite", tamanhoPagina); cmd.Parameters.AddWithValue("@deslocamento", deslocamento);
        using var r = cmd.ExecuteReader(); var lista = new List<RegistroAuditoria>();
        while (r.Read()) lista.Add(new(r.GetInt64(0), DateTime.SpecifyKind(r.GetDateTime(1), DateTimeKind.Utc), r.GetString(2),
            r.IsDBNull(3) ? null : r.GetString(3), r.GetString(4), r.GetInt64(5), r.GetString(6), r.GetString(7)));
        return new(lista, total);

        void Parametros(MySqlCommand q)
        {
            q.Parameters.AddWithValue("@inicio", inicioUtc); q.Parameters.AddWithValue("@fim", fimUtc);
            q.Parameters.AddWithValue("@entidade", entidade); q.Parameters.AddWithValue("@tipo", tipo);
            q.Parameters.AddWithValue("@autor", string.IsNullOrWhiteSpace(autor) ? null : autor.Trim());
        }
    }

    public List<DetalheAuditoria> Detalhes(long logId)
    {
        if (logId <= 0) throw new ArgumentException("Selecione um registro do histórico.");
        using var c = Conexao.Abrir();
        using var cmd = new MySqlCommand("SELECT Campo, ValorAnterior, ValorNovo FROM DetalheLogAuditoria WHERE LogAuditoriaId = @id ORDER BY Campo", c);
        cmd.Parameters.AddWithValue("@id", logId); using var r = cmd.ExecuteReader(); var lista = new List<DetalheAuditoria>();
        while (r.Read()) lista.Add(new(r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        return lista;
    }
}
