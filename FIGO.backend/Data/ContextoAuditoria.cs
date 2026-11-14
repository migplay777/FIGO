namespace FIGO.backend;

public static class ContextoAuditoria
{
    private static readonly AsyncLocal<UsuarioAutenticado?> atual = new();
    public static UsuarioAutenticado? Usuario => atual.Value;
    public static void DefinirUsuario(UsuarioAutenticado? usuario) => atual.Value = usuario;
}
