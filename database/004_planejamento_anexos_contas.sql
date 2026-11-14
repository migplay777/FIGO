-- FIGO | 004 | Anexos, orçamentos, metas e contas a pagar | MySQL 8.0.16+ / 8.4
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

DROP TRIGGER IF EXISTS Figo_Audit_OrcamentoViagem_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_OrcamentoViagem_INSERT AFTER INSERT ON OrcamentoViagem
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('OrcamentoViagem',NEW.Id,1,NULL,JSON_OBJECT('ViagemId', NEW.ViagemId, 'CriadoEm', NEW.CriadoEm));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_OrcamentoViagem_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_OrcamentoViagem_UPDATE AFTER UPDATE ON OrcamentoViagem
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('OrcamentoViagem',NEW.Id,2,JSON_OBJECT('ViagemId', OLD.ViagemId, 'CriadoEm', OLD.CriadoEm),JSON_OBJECT('ViagemId', NEW.ViagemId, 'CriadoEm', NEW.CriadoEm));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_OrcamentoViagem_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_OrcamentoViagem_DELETE AFTER DELETE ON OrcamentoViagem
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('OrcamentoViagem',OLD.Id,3,JSON_OBJECT('ViagemId', OLD.ViagemId, 'CriadoEm', OLD.CriadoEm),NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ItemOrcamento_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ItemOrcamento_INSERT AFTER INSERT ON ItemOrcamento
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('ItemOrcamento',NEW.Id,1,NULL,JSON_OBJECT('OrcamentoViagemId', NEW.OrcamentoViagemId, 'CategoriaDespesaId', NEW.CategoriaDespesaId, 'ValorPrevisto', NEW.ValorPrevisto));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ItemOrcamento_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ItemOrcamento_UPDATE AFTER UPDATE ON ItemOrcamento
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('ItemOrcamento',NEW.Id,2,JSON_OBJECT('OrcamentoViagemId', OLD.OrcamentoViagemId, 'CategoriaDespesaId', OLD.CategoriaDespesaId, 'ValorPrevisto', OLD.ValorPrevisto),JSON_OBJECT('OrcamentoViagemId', NEW.OrcamentoViagemId, 'CategoriaDespesaId', NEW.CategoriaDespesaId, 'ValorPrevisto', NEW.ValorPrevisto));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ItemOrcamento_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ItemOrcamento_DELETE AFTER DELETE ON ItemOrcamento
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('ItemOrcamento',OLD.Id,3,JSON_OBJECT('OrcamentoViagemId', OLD.OrcamentoViagemId, 'CategoriaDespesaId', OLD.CategoriaDespesaId, 'ValorPrevisto', OLD.ValorPrevisto),NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_MetaArrecadacao_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_MetaArrecadacao_INSERT AFTER INSERT ON MetaArrecadacao
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('MetaArrecadacao',NEW.Id,1,NULL,JSON_OBJECT('ViagemId', NEW.ViagemId, 'Descricao', NEW.Descricao, 'ValorMeta', NEW.ValorMeta, 'DataLimite', NEW.DataLimite));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_MetaArrecadacao_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_MetaArrecadacao_UPDATE AFTER UPDATE ON MetaArrecadacao
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('MetaArrecadacao',NEW.Id,2,JSON_OBJECT('ViagemId', OLD.ViagemId, 'Descricao', OLD.Descricao, 'ValorMeta', OLD.ValorMeta, 'DataLimite', OLD.DataLimite),JSON_OBJECT('ViagemId', NEW.ViagemId, 'Descricao', NEW.Descricao, 'ValorMeta', NEW.ValorMeta, 'DataLimite', NEW.DataLimite));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_MetaArrecadacao_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_MetaArrecadacao_DELETE AFTER DELETE ON MetaArrecadacao
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('MetaArrecadacao',OLD.Id,3,JSON_OBJECT('ViagemId', OLD.ViagemId, 'Descricao', OLD.Descricao, 'ValorMeta', OLD.ValorMeta, 'DataLimite', OLD.DataLimite),NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ContaPagar_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ContaPagar_INSERT AFTER INSERT ON ContaPagar
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('ContaPagar',NEW.Id,1,NULL,JSON_OBJECT('ViagemId', NEW.ViagemId, 'Descricao', NEW.Descricao, 'Credor', NEW.Credor, 'CategoriaDespesaId', NEW.CategoriaDespesaId, 'ValorTotal', NEW.ValorTotal, 'Vencimento', NEW.Vencimento));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ContaPagar_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ContaPagar_UPDATE AFTER UPDATE ON ContaPagar
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('ContaPagar',NEW.Id,2,JSON_OBJECT('ViagemId', OLD.ViagemId, 'Descricao', OLD.Descricao, 'Credor', OLD.Credor, 'CategoriaDespesaId', OLD.CategoriaDespesaId, 'ValorTotal', OLD.ValorTotal, 'Vencimento', OLD.Vencimento),JSON_OBJECT('ViagemId', NEW.ViagemId, 'Descricao', NEW.Descricao, 'Credor', NEW.Credor, 'CategoriaDespesaId', NEW.CategoriaDespesaId, 'ValorTotal', NEW.ValorTotal, 'Vencimento', NEW.Vencimento));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ContaPagar_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ContaPagar_DELETE AFTER DELETE ON ContaPagar
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('ContaPagar',OLD.Id,3,JSON_OBJECT('ViagemId', OLD.ViagemId, 'Descricao', OLD.Descricao, 'Credor', OLD.Credor, 'CategoriaDespesaId', OLD.CategoriaDespesaId, 'ValorTotal', OLD.ValorTotal, 'Vencimento', OLD.Vencimento),NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_PagamentoConta_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_PagamentoConta_INSERT AFTER INSERT ON PagamentoConta
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('PagamentoConta',NEW.Id,1,NULL,JSON_OBJECT('ContaPagarId', NEW.ContaPagarId, 'DespesaId', NEW.DespesaId, 'Referencia', NEW.Referencia, 'ChaveOperacao', NEW.ChaveOperacao));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_PagamentoConta_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_PagamentoConta_UPDATE AFTER UPDATE ON PagamentoConta
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('PagamentoConta',NEW.Id,2,JSON_OBJECT('ContaPagarId', OLD.ContaPagarId, 'DespesaId', OLD.DespesaId, 'Referencia', OLD.Referencia, 'ChaveOperacao', OLD.ChaveOperacao),JSON_OBJECT('ContaPagarId', NEW.ContaPagarId, 'DespesaId', NEW.DespesaId, 'Referencia', NEW.Referencia, 'ChaveOperacao', NEW.ChaveOperacao));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_PagamentoConta_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_PagamentoConta_DELETE AFTER DELETE ON PagamentoConta
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('PagamentoConta',OLD.Id,3,JSON_OBJECT('ContaPagarId', OLD.ContaPagarId, 'DespesaId', OLD.DespesaId, 'Referencia', OLD.Referencia, 'ChaveOperacao', OLD.ChaveOperacao),NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_AnexoLancamento_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_AnexoLancamento_INSERT AFTER INSERT ON AnexoLancamento
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('AnexoLancamento',NEW.Id,1,NULL,JSON_OBJECT('ReceitaId', NEW.ReceitaId, 'DespesaId', NEW.DespesaId, 'NomeOriginal', NEW.NomeOriginal, 'TipoArquivo', NEW.TipoArquivo, 'TamanhoBytes', NEW.TamanhoBytes, 'Sha256', NEW.Sha256));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_AnexoLancamento_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_AnexoLancamento_UPDATE AFTER UPDATE ON AnexoLancamento
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('AnexoLancamento',NEW.Id,2,JSON_OBJECT('ReceitaId', OLD.ReceitaId, 'DespesaId', OLD.DespesaId, 'NomeOriginal', OLD.NomeOriginal, 'TipoArquivo', OLD.TipoArquivo, 'TamanhoBytes', OLD.TamanhoBytes, 'Sha256', OLD.Sha256),JSON_OBJECT('ReceitaId', NEW.ReceitaId, 'DespesaId', NEW.DespesaId, 'NomeOriginal', NEW.NomeOriginal, 'TipoArquivo', NEW.TipoArquivo, 'TamanhoBytes', NEW.TamanhoBytes, 'Sha256', NEW.Sha256));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_AnexoLancamento_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_AnexoLancamento_DELETE AFTER DELETE ON AnexoLancamento
FOR EACH ROW BEGIN
 CALL Figo_RegistrarAuditoria('AnexoLancamento',OLD.Id,3,JSON_OBJECT('ReceitaId', OLD.ReceitaId, 'DespesaId', OLD.DespesaId, 'NomeOriginal', OLD.NomeOriginal, 'TipoArquivo', OLD.TipoArquivo, 'TamanhoBytes', OLD.TamanhoBytes, 'Sha256', OLD.Sha256),NULL);
END$$
DELIMITER ;

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
 SELECT COALESCE(SUM(d.Valor),0) INTO v_pago FROM PagamentoConta p JOIN Despesa d ON d.Id=p.DespesaId WHERE p.ContaPagarId=NEW.ContaPagarId FOR UPDATE;
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
