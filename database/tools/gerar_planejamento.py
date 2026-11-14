"""Gera a migração 004 e recompõe o instalador vazio. Execute da raiz do projeto."""
from pathlib import Path
import runpy

root=Path(__file__).resolve().parents[2]
sql=r"""-- FIGO | 004 | Anexos, orçamentos, metas e contas a pagar | MySQL 8.0.16+ / 8.4
-- Aplique após 001, 002 e 003, com backup e aplicativo fechado.
-- Reexecutável. Não altera lançamentos legados nem cadastra valores fictícios.
SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS OrcamentoViagem (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ViagemId INT NOT NULL,
    CriadoEm TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY UQ_OrcamentoViagem_Viagem (ViagemId),
    CONSTRAINT FK_OrcamentoViagem_Viagem FOREIGN KEY (ViagemId) REFERENCES Viagem(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS ItemOrcamento (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OrcamentoViagemId INT NOT NULL,
    CategoriaDespesaId INT NOT NULL,
    ValorPrevisto DECIMAL(12,2) NOT NULL,
    UNIQUE KEY UQ_ItemOrcamento_Categoria (OrcamentoViagemId,CategoriaDespesaId),
    CONSTRAINT CK_ItemOrcamento_Valor CHECK (ValorPrevisto>0 AND ValorPrevisto<=999999999.99),
    CONSTRAINT FK_ItemOrcamento_Orcamento FOREIGN KEY (OrcamentoViagemId) REFERENCES OrcamentoViagem(Id),
    CONSTRAINT FK_ItemOrcamento_Categoria FOREIGN KEY (CategoriaDespesaId) REFERENCES CategoriaDespesa(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS MetaArrecadacao (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ViagemId INT NOT NULL,
    Descricao VARCHAR(150) NOT NULL,
    ValorMeta DECIMAL(12,2) NOT NULL,
    DataLimite DATE NULL,
    UNIQUE KEY UQ_MetaArrecadacao_Viagem (ViagemId),
    CONSTRAINT CK_MetaArrecadacao_Valor CHECK (ValorMeta>0 AND ValorMeta<=999999999.99),
    CONSTRAINT CK_MetaArrecadacao_Nome CHECK (CHAR_LENGTH(TRIM(Descricao))>0),
    CONSTRAINT CK_MetaArrecadacao_Data CHECK (DataLimite IS NULL OR DataLimite>='1900-01-01'),
    CONSTRAINT FK_MetaArrecadacao_Viagem FOREIGN KEY (ViagemId) REFERENCES Viagem(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS ContaPagar (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ViagemId INT NOT NULL,
    Descricao VARCHAR(255) NOT NULL,
    Credor VARCHAR(150) NOT NULL,
    CategoriaDespesaId INT NOT NULL,
    ValorTotal DECIMAL(12,2) NOT NULL,
    Vencimento DATE NOT NULL,
    KEY IX_ContaPagar_ViagemVencimento (ViagemId,Vencimento),
    CONSTRAINT CK_ContaPagar_Valor CHECK (ValorTotal>0 AND ValorTotal<=999999999.99),
    CONSTRAINT CK_ContaPagar_Descricao CHECK (CHAR_LENGTH(TRIM(Descricao))>0),
    CONSTRAINT CK_ContaPagar_Credor CHECK (CHAR_LENGTH(TRIM(Credor))>0),
    CONSTRAINT CK_ContaPagar_Data CHECK (Vencimento>='1900-01-01'),
    CONSTRAINT FK_ContaPagar_Viagem FOREIGN KEY (ViagemId) REFERENCES Viagem(Id),
    CONSTRAINT FK_ContaPagar_Categoria FOREIGN KEY (CategoriaDespesaId) REFERENCES CategoriaDespesa(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS PagamentoConta (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ContaPagarId INT NOT NULL,
    DespesaId INT NOT NULL,
    Referencia VARCHAR(100) NOT NULL DEFAULT '',
    ChaveOperacao CHAR(32) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    UNIQUE KEY UQ_PagamentoConta_Despesa (DespesaId),
    UNIQUE KEY UQ_PagamentoConta_Operacao (ChaveOperacao),
    CONSTRAINT FK_PagamentoConta_Conta FOREIGN KEY (ContaPagarId) REFERENCES ContaPagar(Id),
    CONSTRAINT FK_PagamentoConta_Despesa FOREIGN KEY (DespesaId) REFERENCES Despesa(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS AnexoLancamento (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ReceitaId INT NULL,
    DespesaId INT NULL,
    NomeOriginal VARCHAR(180) NOT NULL,
    TipoArquivo VARCHAR(30) NOT NULL,
    TamanhoBytes INT NOT NULL,
    Sha256 CHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    Conteudo MEDIUMBLOB NOT NULL,
    UNIQUE KEY UQ_AnexoLancamento_Receita (ReceitaId,Sha256),
    UNIQUE KEY UQ_AnexoLancamento_Despesa (DespesaId,Sha256),
    CONSTRAINT CK_AnexoLancamento_Destino CHECK ((ReceitaId IS NULL)<>(DespesaId IS NULL)),
    CONSTRAINT CK_AnexoLancamento_Tamanho CHECK (TamanhoBytes BETWEEN 1 AND 5242880 AND OCTET_LENGTH(Conteudo)=TamanhoBytes),
    CONSTRAINT CK_AnexoLancamento_Tipo CHECK (TipoArquivo IN ('application/pdf','image/png','image/jpeg')),
    CONSTRAINT FK_AnexoLancamento_Receita FOREIGN KEY (ReceitaId) REFERENCES Receita(Id),
    CONSTRAINT FK_AnexoLancamento_Despesa FOREIGN KEY (DespesaId) REFERENCES Despesa(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
"""
fields={
 'OrcamentoViagem':'ViagemId CriadoEm',
 'ItemOrcamento':'OrcamentoViagemId CategoriaDespesaId ValorPrevisto',
 'MetaArrecadacao':'ViagemId Descricao ValorMeta DataLimite',
 'ContaPagar':'ViagemId Descricao Credor CategoriaDespesaId ValorTotal Vencimento',
 'PagamentoConta':'ContaPagarId DespesaId Referencia ChaveOperacao',
 'AnexoLancamento':'ReceitaId DespesaId NomeOriginal TipoArquivo TamanhoBytes Sha256',
}
for table,columns in fields.items():
 for op,code in [('INSERT',1),('UPDATE',2),('DELETE',3)]:
  name=f'Figo_Audit_{table}_{op}'
  def obj(prefix):return 'JSON_OBJECT('+', '.join(f"'{col}', {prefix}.{col}" for col in columns.split())+')'
  before=obj('OLD') if op!='INSERT' else 'NULL';after=obj('NEW') if op!='DELETE' else 'NULL';prefix='OLD' if op=='DELETE' else 'NEW'
  sql+=f"\nDROP TRIGGER IF EXISTS {name};\nDELIMITER $$\nCREATE TRIGGER {name} AFTER {op} ON {table}\nFOR EACH ROW BEGIN\n CALL Figo_RegistrarAuditoria('{table}',{prefix}.Id,{code},{before},{after});\nEND$$\nDELIMITER ;\n"
sql+=r"""
-- Valores/data do pagamento vivem na Despesa, sem duplicar totais financeiros.
DROP TRIGGER IF EXISTS Figo_Pagamento_Validar;
DROP TRIGGER IF EXISTS Figo_Pagamento_Imutavel;
DROP TRIGGER IF EXISTS Figo_Pagamento_Estornar;
DROP TRIGGER IF EXISTS Figo_Pagamento_TravarEstorno;
DROP TRIGGER IF EXISTS Figo_ContaPagar_ValidarEdicao;
DROP TRIGGER IF EXISTS Figo_Despesa_ProtegerEdicao;
DROP TRIGGER IF EXISTS Figo_Despesa_ProtegerExclusao;
DROP TRIGGER IF EXISTS Figo_Anexo_VerificarInsercao;
DROP TRIGGER IF EXISTS Figo_Anexo_VerificarEdicao;
DELIMITER $$
CREATE TRIGGER Figo_Anexo_VerificarInsercao BEFORE INSERT ON AnexoLancamento
FOR EACH ROW BEGIN
 IF BINARY UPPER(NEW.Sha256)<>BINARY UPPER(SHA2(NEW.Conteudo,256)) THEN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Hash do comprovante incompativel com o conteudo.';
 END IF;
END$$
CREATE TRIGGER Figo_Anexo_VerificarEdicao BEFORE UPDATE ON AnexoLancamento
FOR EACH ROW BEGIN
 IF BINARY UPPER(NEW.Sha256)<>BINARY UPPER(SHA2(NEW.Conteudo,256)) THEN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Hash do comprovante incompativel com o conteudo.';
 END IF;
END$$
CREATE TRIGGER Figo_Pagamento_Validar BEFORE INSERT ON PagamentoConta
FOR EACH ROW BEGIN
 DECLARE v_viagem INT; DECLARE v_categoria INT; DECLARE v_total DECIMAL(12,2);
 DECLARE v_dviagem INT; DECLARE v_dcategoria INT; DECLARE v_valor DECIMAL(12,2); DECLARE v_pago DECIMAL(14,2);
 SELECT ViagemId,CategoriaDespesaId,ValorTotal INTO v_viagem,v_categoria,v_total FROM ContaPagar WHERE Id=NEW.ContaPagarId FOR UPDATE;
 SELECT ViagemId,CategoriaId,Valor INTO v_dviagem,v_dcategoria,v_valor FROM Despesa WHERE Id=NEW.DespesaId FOR UPDATE;
 -- Leitura atual, bloqueando apenas Despesa: PagamentoConta já é alvo do INSERT.
 SELECT COALESCE(SUM(d.Valor),0) INTO v_pago FROM PagamentoConta p JOIN Despesa d ON d.Id=p.DespesaId WHERE p.ContaPagarId=NEW.ContaPagarId FOR UPDATE OF d;
 IF v_viagem IS NULL OR v_dviagem IS NULL OR NOT(v_viagem<=>v_dviagem) OR NOT(v_categoria<=>v_dcategoria) OR v_valor<=0 OR v_pago+v_valor>v_total THEN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Pagamento invalido: confira viagem, categoria e saldo da conta.';
 END IF;
END$$
CREATE TRIGGER Figo_Pagamento_Imutavel BEFORE UPDATE ON PagamentoConta
FOR EACH ROW BEGIN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Para corrigir um pagamento, estorne e registre novamente em Contas a pagar.';
END$$
CREATE TRIGGER Figo_Pagamento_TravarEstorno BEFORE DELETE ON PagamentoConta
FOR EACH ROW BEGIN
 DECLARE v_id INT;
 SELECT Id INTO v_id FROM ContaPagar WHERE Id=OLD.ContaPagarId FOR UPDATE;
END$$
CREATE TRIGGER Figo_Pagamento_Estornar AFTER DELETE ON PagamentoConta
FOR EACH ROW BEGIN
 DELETE FROM Despesa WHERE Id=OLD.DespesaId;
END$$
CREATE TRIGGER Figo_ContaPagar_ValidarEdicao BEFORE UPDATE ON ContaPagar
FOR EACH ROW BEGIN
 DECLARE v_pago DECIMAL(14,2); DECLARE v_quantidade INT;
 SELECT COALESCE(SUM(d.Valor),0),COUNT(*) INTO v_pago,v_quantidade FROM PagamentoConta p JOIN Despesa d ON d.Id=p.DespesaId WHERE p.ContaPagarId=OLD.Id FOR UPDATE;
 IF NEW.ViagemId<>OLD.ViagemId OR NEW.ValorTotal<v_pago OR (v_quantidade>0 AND NEW.CategoriaDespesaId<>OLD.CategoriaDespesaId) THEN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Conta com pagamentos: preserve a viagem/categoria e nao reduza o total abaixo do valor pago.';
 END IF;
END$$
CREATE TRIGGER Figo_Despesa_ProtegerEdicao BEFORE UPDATE ON Despesa
FOR EACH ROW BEGIN
 IF EXISTS(SELECT 1 FROM PagamentoConta WHERE DespesaId=OLD.Id) THEN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Esta despesa pertence a um pagamento. Use o estorno em Contas a pagar.';
 END IF;
END$$
CREATE TRIGGER Figo_Despesa_ProtegerExclusao BEFORE DELETE ON Despesa
FOR EACH ROW BEGIN
 IF EXISTS(SELECT 1 FROM PagamentoConta WHERE DespesaId=OLD.Id) THEN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Esta despesa pertence a um pagamento. Use o estorno em Contas a pagar.';
 END IF;
END$$
DELIMITER ;
"""
(root/'database/004_planejamento_anexos_contas.sql').write_text(sql,encoding='utf-8')
runpy.run_path(str(root/'database/tools/gerar_participantes_auditoria.py'))
print('004: 6 novas tabelas; total 21; 54 triggers de auditoria + 9 de integridade.')
