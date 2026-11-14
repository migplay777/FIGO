using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace FIGO.backend;

public sealed class AnexosService
{
    public const int LimiteBytes = 5 * 1024 * 1024;
    public static (string Nome, string Tipo) Validar(string nome, byte[] conteudo)
    {
        nome = Path.GetFileName(nome.Replace('\\','/')).Trim();
        nome = Regex.Replace(nome, "[<>:\"/\\\\|?*\\x00-\\x1f]", "_");
        if(nome.Length is 0 or > 180)throw new ArgumentException("Nome de arquivo inválido (máximo 180 caracteres).");
        if(conteudo.Length is 0 or > LimiteBytes)throw new ArgumentException("O comprovante deve ter entre 1 byte e 5 MB.");
        string ext=Path.GetExtension(nome).ToLowerInvariant();
        bool pdf=conteudo.AsSpan().StartsWith("%PDF-"u8);
        bool png=conteudo.AsSpan().StartsWith(new byte[]{137,80,78,71,13,10,26,10});
        bool jpeg=conteudo.AsSpan().StartsWith(new byte[]{255,216,255});
        string tipo=ext switch { ".pdf" when pdf=>"application/pdf", ".png" when png=>"image/png", ".jpg" or ".jpeg" when jpeg=>"image/jpeg", _=>throw new ArgumentException("Selecione um PDF, PNG ou JPEG com conteúdo compatível com a extensão.") };
        return(nome,tipo);
    }
    public static async Task<byte[]> LerArquivoAsync(string caminho)
    {
        using var f=new FileStream(caminho,FileMode.Open,FileAccess.Read,FileShare.Read,81920,true);
        if(f.Length>LimiteBytes)throw new ArgumentException("O arquivo excede o limite de 5 MB.");
        using var dados=new MemoryStream();var buffer=new byte[81920];int lidos;
        while((lidos=await f.ReadAsync(buffer))>0)
        {
            if(dados.Length+lidos>LimiteBytes)throw new ArgumentException("O arquivo excede o limite de 5 MB.");
            await dados.WriteAsync(buffer.AsMemory(0,lidos));
        }
        return dados.ToArray();
    }
    public int Adicionar(bool receita, int lancamentoId, string nome, byte[] conteudo)
    {
        ValidacaoPlanejamento.Id(lancamentoId,"um lançamento");var arquivo=Validar(nome,conteudo);
        string hash=Convert.ToHexString(SHA256.HashData(conteudo));
        using var c=Conexao.Abrir();using var cmd=new MySqlCommand("INSERT INTO AnexoLancamento (ReceitaId,DespesaId,NomeOriginal,TipoArquivo,TamanhoBytes,Sha256,Conteudo) VALUES (@receita,@despesa,@nome,@tipo,@tamanho,@hash,@conteudo)",c);
        cmd.Parameters.AddWithValue("@receita",receita?lancamentoId:null);cmd.Parameters.AddWithValue("@despesa",receita?null:lancamentoId);cmd.Parameters.AddWithValue("@nome",arquivo.Nome);cmd.Parameters.AddWithValue("@tipo",arquivo.Tipo);cmd.Parameters.AddWithValue("@tamanho",conteudo.Length);cmd.Parameters.AddWithValue("@hash",hash);cmd.Parameters.AddWithValue("@conteudo",conteudo);
        try{cmd.ExecuteNonQuery();return checked((int)cmd.LastInsertedId);}
        catch(MySqlException ex) when(ex.Number==1062){throw new ArgumentException("Este arquivo já está anexado a esse lançamento.");}
    }
    public List<AnexoResumo> Listar(bool receita,int lancamentoId)
    {
        ValidacaoPlanejamento.Id(lancamentoId,"um lançamento");using var c=Conexao.Abrir();
        using var cmd=new MySqlCommand("SELECT Id,NomeOriginal,TipoArquivo,TamanhoBytes,Sha256 FROM AnexoLancamento WHERE "+(receita?"ReceitaId":"DespesaId")+"=@id ORDER BY Id",c);
        cmd.Parameters.AddWithValue("@id",lancamentoId);using var r=cmd.ExecuteReader();var lista=new List<AnexoResumo>();
        while(r.Read())lista.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetInt32(3),r.GetString(4)));return lista;
    }
    public ArquivoAnexo Baixar(int id)
    {
        ValidacaoPlanejamento.Id(id,"um anexo");using var c=Conexao.Abrir();
        using var cmd=new MySqlCommand("SELECT NomeOriginal,TipoArquivo,TamanhoBytes,Sha256,Conteudo FROM AnexoLancamento WHERE Id=@id",c);cmd.Parameters.AddWithValue("@id",id);
        using var r=cmd.ExecuteReader();if(!r.Read())throw new ArgumentException("O anexo não existe mais.");
        var info=new AnexoResumo(id,r.GetString(0),r.GetString(1),r.GetInt32(2),r.GetString(3));byte[] bytes=(byte[])r[4];
        if(bytes.Length!=info.TamanhoBytes||!Convert.ToHexString(SHA256.HashData(bytes)).Equals(info.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("O comprovante não passou na verificação de integridade.");
        var valido=Validar(info.NomeOriginal,bytes);
        if(valido.Tipo!=info.TipoArquivo)throw new InvalidDataException("Tipo do comprovante incompatível com os metadados.");
        return new(info with { NomeOriginal=valido.Nome },bytes);
    }
    public void Excluir(int id,bool receita,int lancamentoId)
    {
        ValidacaoPlanejamento.Id(id,"um anexo");ValidacaoPlanejamento.Id(lancamentoId,"um lançamento");using var c=Conexao.Abrir();
        using var cmd=new MySqlCommand("DELETE FROM AnexoLancamento WHERE Id=@id AND "+(receita?"ReceitaId":"DespesaId")+"=@lancamento",c);cmd.Parameters.AddWithValue("@id",id);cmd.Parameters.AddWithValue("@lancamento",lancamentoId);cmd.ExecuteNonQuery();
    }
}
