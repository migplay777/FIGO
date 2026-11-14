using MySqlConnector;

namespace FIGO.Desktop;
internal static class ErroFinanceiro
{
    public static string Mensagem(Exception ex) => ex switch
    {
        ArgumentException => ex.Message,
        MySqlException mysql when mysql.Number is 1146 or 1054 or 1305 => "Atualize o banco com as migrações 001, 002, 003 e 004 da pasta database, nessa ordem.",
        MySqlException mysql when mysql.Number == 1644 => mysql.Message,
        MySqlException mysql when mysql.Number == 1451 => "Há registros vinculados. Remova os anexos ou estorne os pagamentos antes de excluir; o histórico será preservado.",
        MySqlException mysql when mysql.Number == 1452 => "Um dos registros selecionados não existe mais. Atualize os cadastros e tente novamente.",
        MySqlException => "Não foi possível acessar o MySQL. Verifique a conexão e tente novamente.",
        InvalidDataException => ex.Message,
        IOException or UnauthorizedAccessException => "Não foi possível ler ou salvar o arquivo. Verifique o local, o uso por outro programa e as permissões.",
        InvalidOperationException => "Verifique a configuração FIGO_DB_CONNECTION e a preparação do banco.",
        _ => "Não foi possível concluir a operação. Tente novamente."
    };
}
