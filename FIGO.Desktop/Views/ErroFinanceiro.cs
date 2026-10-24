using MySqlConnector;

namespace FIGO.Desktop;
internal static class ErroFinanceiro
{
    public static string Mensagem(Exception ex) => ex switch
    {
        ArgumentException => ex.Message,
        MySqlException mysql when mysql.Number is 1146 or 1054 => "O banco precisa ser atualizado. Execute database/002_cadastros_financeiros.sql no banco do FIGO.",
        MySqlException mysql when mysql.Number == 1452 => "Um dos registros selecionados não existe mais. Atualize os cadastros e tente novamente.",
        MySqlException => "Não foi possível acessar o MySQL. Verifique a conexão e tente novamente.",
        InvalidOperationException => "Verifique a configuração FIGO_DB_CONNECTION e a preparação do banco.",
        _ => "Não foi possível concluir a operação. Tente novamente."
    };
}
