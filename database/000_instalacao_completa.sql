-- FIGO | Instalação completa vazia | MySQL 8.0.16+ / 8.4
-- Para banco existente, prefira as migrações individuais; consulte docs/PARTICIPANTES_AUDITORIA.md.
CREATE DATABASE IF NOT EXISTS figo CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE figo;
CREATE TABLE IF NOT EXISTS Viagem (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(255) NOT NULL, Destino VARCHAR(255) NOT NULL,
    DataInicio DATE NOT NULL, DataFim DATE NOT NULL, QuantidadeParticipantes INT NOT NULL
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS Receita (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ViagemId INT NOT NULL, Descricao VARCHAR(255) NOT NULL, Valor DECIMAL(12,2) NOT NULL,
    Data DATE NOT NULL, Origem VARCHAR(255) NOT NULL,
    FOREIGN KEY (ViagemId) REFERENCES Viagem(Id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS Despesa (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ViagemId INT NOT NULL, Descricao VARCHAR(255) NOT NULL, Valor DECIMAL(12,2) NOT NULL,
    Data DATE NOT NULL, Categoria VARCHAR(255) NOT NULL,
    FOREIGN KEY (ViagemId) REFERENCES Viagem(Id)
) ENGINE=InnoDB;
CREATE OR REPLACE VIEW vw_ResumoViagem AS
SELECT v.Id AS ViagemId, v.Nome, v.Destino, v.QuantidadeParticipantes,
    COALESCE(r.Total, 0) AS TotalReceitas, COALESCE(d.Total, 0) AS TotalDespesas,
    COALESCE(r.Total, 0) - COALESCE(d.Total, 0) AS Saldo
FROM Viagem v
LEFT JOIN (SELECT ViagemId, SUM(Valor) AS Total FROM Receita GROUP BY ViagemId) r ON r.ViagemId = v.Id
LEFT JOIN (SELECT ViagemId, SUM(Valor) AS Total FROM Despesa GROUP BY ViagemId) d ON d.ViagemId = v.Id;
-- Execute no mesmo banco configurado em FIGO_DB_CONNECTION, antes do primeiro login.
-- Não altera as tabelas financeiras existentes.
CREATE TABLE IF NOT EXISTS Usuarios (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    NomeUsuario VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
    SenhaHash VARBINARY(32) NOT NULL,
    Salt VARBINARY(16) NOT NULL,
    Iteracoes INT NOT NULL,
    CriadoEm TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY UQ_Usuarios_NomeUsuario (NomeUsuario)
);

-- Selecione o banco do FIGO antes de executar (ex.: USE figo;).
-- Execute o arquivo completo no MySQL Workbench. Pode ser executado novamente.
-- Não cria pagadores, formas ou contas fictícias para receitas antigas.
CREATE TABLE IF NOT EXISTS Pagador (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Tipo ENUM('PF', 'PJ') NOT NULL,
    Nome VARCHAR(150) NOT NULL,
    Documento VARCHAR(14) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
    UNIQUE KEY UQ_Pagador_Documento (Tipo, Documento),
    KEY IX_Pagador_Nome (Nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS OrigemReceita (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(255) NOT NULL,
    UNIQUE KEY UQ_OrigemReceita_Nome (Nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS FormaPagamento (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    UNIQUE KEY UQ_FormaPagamento_Nome (Nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ContaFinanceira (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Tipo ENUM('Bancaria', 'Caixa') NOT NULL,
    Banco VARCHAR(100) NOT NULL DEFAULT '',
    Agencia VARCHAR(20) NOT NULL DEFAULT '',
    Numero VARCHAR(30) NOT NULL DEFAULT '',
    Titular VARCHAR(150) NOT NULL DEFAULT '',
    ChaveBancaria VARCHAR(160) GENERATED ALWAYS AS
        (CASE WHEN Tipo = 'Bancaria' THEN CONCAT(Banco, '|', Agencia, '|', Numero) ELSE NULL END) STORED,
    UNIQUE KEY UQ_ContaFinanceira_Nome (Nome),
    UNIQUE KEY UQ_ContaFinanceira_Banco (ChaveBancaria)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS CategoriaDespesa (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(255) NOT NULL,
    UNIQUE KEY UQ_CategoriaDespesa_Nome (Nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

DROP PROCEDURE IF EXISTS Figo_Migrar_002;
DELIMITER $$
CREATE PROCEDURE Figo_Migrar_002()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Receita' AND COLUMN_NAME = 'OrigemId') THEN
        ALTER TABLE Receita ADD COLUMN OrigemId INT NULL,
            ADD CONSTRAINT FK_Receita_Origem FOREIGN KEY (OrigemId) REFERENCES OrigemReceita(Id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Receita' AND COLUMN_NAME = 'PagadorId') THEN
        ALTER TABLE Receita ADD COLUMN PagadorId INT NULL,
            ADD CONSTRAINT FK_Receita_Pagador FOREIGN KEY (PagadorId) REFERENCES Pagador(Id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Receita' AND COLUMN_NAME = 'FormaPagamentoId') THEN
        ALTER TABLE Receita ADD COLUMN FormaPagamentoId INT NULL,
            ADD CONSTRAINT FK_Receita_Forma FOREIGN KEY (FormaPagamentoId) REFERENCES FormaPagamento(Id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Receita' AND COLUMN_NAME = 'ContaFinanceiraId') THEN
        ALTER TABLE Receita ADD COLUMN ContaFinanceiraId INT NULL,
            ADD CONSTRAINT FK_Receita_Conta FOREIGN KEY (ContaFinanceiraId) REFERENCES ContaFinanceira(Id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Receita' AND COLUMN_NAME = 'ReferenciaPagamento') THEN
        ALTER TABLE Receita ADD COLUMN ReferenciaPagamento VARCHAR(100) NOT NULL DEFAULT '';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Despesa' AND COLUMN_NAME = 'CategoriaId') THEN
        ALTER TABLE Despesa ADD COLUMN CategoriaId INT NULL,
            ADD CONSTRAINT FK_Despesa_Categoria FOREIGN KEY (CategoriaId) REFERENCES CategoriaDespesa(Id);
    END IF;
END$$
DELIMITER ;
CALL Figo_Migrar_002();
DROP PROCEDURE Figo_Migrar_002;

-- Transforma valores já utilizados em opções de seleção, mantendo o texto original.
INSERT INTO OrigemReceita (Nome)
SELECT DISTINCT CONVERT(TRIM(r.Origem) USING utf8mb4) COLLATE utf8mb4_unicode_ci
FROM Receita r
WHERE TRIM(COALESCE(r.Origem, '')) <> '' AND NOT EXISTS (
    SELECT 1 FROM OrigemReceita o WHERE o.Nome = CONVERT(TRIM(r.Origem) USING utf8mb4) COLLATE utf8mb4_unicode_ci
);
INSERT INTO CategoriaDespesa (Nome)
SELECT DISTINCT CONVERT(TRIM(d.Categoria) USING utf8mb4) COLLATE utf8mb4_unicode_ci
FROM Despesa d
WHERE TRIM(COALESCE(d.Categoria, '')) <> '' AND NOT EXISTS (
    SELECT 1 FROM CategoriaDespesa c WHERE c.Nome = CONVERT(TRIM(d.Categoria) USING utf8mb4) COLLATE utf8mb4_unicode_ci
);
UPDATE Receita r JOIN OrigemReceita o ON o.Nome = CONVERT(TRIM(r.Origem) USING utf8mb4) COLLATE utf8mb4_unicode_ci
SET r.OrigemId = o.Id WHERE r.OrigemId IS NULL;
UPDATE Despesa d JOIN CategoriaDespesa c ON c.Nome = CONVERT(TRIM(d.Categoria) USING utf8mb4) COLLATE utf8mb4_unicode_ci
SET d.CategoriaId = c.Id WHERE d.CategoriaId IS NULL;

INSERT INTO OrigemReceita (Nome)
SELECT s.Nome FROM (SELECT 'Doação' AS Nome UNION ALL SELECT 'Contribuição' UNION ALL SELECT 'Oferta'
    UNION ALL SELECT 'Patrocínio' UNION ALL SELECT 'Venda' UNION ALL SELECT 'Evento' UNION ALL SELECT 'Reembolso' UNION ALL SELECT 'Outros') s
WHERE NOT EXISTS (SELECT 1 FROM OrigemReceita o WHERE o.Nome = s.Nome COLLATE utf8mb4_unicode_ci);
INSERT INTO FormaPagamento (Nome)
SELECT s.Nome FROM (SELECT 'Pix' AS Nome UNION ALL SELECT 'Transferência bancária' UNION ALL SELECT 'TED'
    UNION ALL SELECT 'Boleto' UNION ALL SELECT 'Cartão de crédito' UNION ALL SELECT 'Cartão de débito'
    UNION ALL SELECT 'Dinheiro' UNION ALL SELECT 'Depósito' UNION ALL SELECT 'Cheque') s
WHERE NOT EXISTS (SELECT 1 FROM FormaPagamento f WHERE f.Nome = s.Nome COLLATE utf8mb4_unicode_ci);
INSERT INTO CategoriaDespesa (Nome)
SELECT s.Nome FROM (SELECT 'Alimentação' AS Nome UNION ALL SELECT 'Transporte' UNION ALL SELECT 'Hospedagem'
    UNION ALL SELECT 'Materiais' UNION ALL SELECT 'Outros') s
WHERE NOT EXISTS (SELECT 1 FROM CategoriaDespesa c WHERE c.Nome = s.Nome COLLATE utf8mb4_unicode_ci);
INSERT INTO ContaFinanceira (Nome, Tipo)
SELECT 'Caixa em espécie', 'Caixa'
WHERE NOT EXISTS (SELECT 1 FROM ContaFinanceira WHERE Nome = 'Caixa em espécie');

-- FIGO | 003 | Participantes opcionais e auditoria | MySQL 8.0.16+ / 8.4
-- Banco existente: selecione o banco e aplique 001, 002 e este arquivo completo.
-- Faça backup e mantenha o aplicativo fechado durante a migração (DDL faz commit implícito).
-- Reexecução preserva pessoas, vínculos e logs; recria somente os triggers/procedimento desta versão.
SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS Participante (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    NomeCompleto VARCHAR(150) NOT NULL,
    Cpf CHAR(11) CHARACTER SET ascii COLLATE ascii_bin NULL,
    UNIQUE KEY UQ_Participante_Cpf (Cpf),
    KEY IX_Participante_Nome (NomeCompleto),
    CONSTRAINT CK_Participante_Nome CHECK (CHAR_LENGTH(TRIM(NomeCompleto)) > 0),
    CONSTRAINT CK_Participante_Cpf CHECK (Cpf IS NULL OR Cpf REGEXP '^[0-9]{11}$')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS FuncaoParticipante (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    UNIQUE KEY UQ_FuncaoParticipante_Nome (Nome),
    CONSTRAINT CK_FuncaoParticipante_Nome CHECK (CHAR_LENGTH(TRIM(Nome)) > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS ViagemParticipante (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ViagemId INT NOT NULL,
    ParticipanteId INT NOT NULL,
    Idade INT NULL,
    FuncaoParticipanteId INT NULL,
    UNIQUE KEY UQ_ViagemParticipante (ViagemId, ParticipanteId),
    KEY IX_ViagemParticipante_Pessoa (ParticipanteId),
    CONSTRAINT CK_ViagemParticipante_Idade CHECK (Idade IS NULL OR Idade BETWEEN 0 AND 130),
    CONSTRAINT FK_ViagemParticipante_Viagem FOREIGN KEY (ViagemId) REFERENCES Viagem(Id),
    CONSTRAINT FK_ViagemParticipante_Pessoa FOREIGN KEY (ParticipanteId) REFERENCES Participante(Id),
    CONSTRAINT FK_ViagemParticipante_Funcao FOREIGN KEY (FuncaoParticipanteId) REFERENCES FuncaoParticipante(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS TipoEventoAuditoria (
    Id SMALLINT NOT NULL PRIMARY KEY,
    Codigo VARCHAR(20) NOT NULL,
    Nome VARCHAR(50) NOT NULL,
    UNIQUE KEY UQ_TipoEventoAuditoria_Codigo (Codigo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS LogAuditoria (
    Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    OcorridoEm DATETIME(6) NOT NULL,
    TipoEventoAuditoriaId SMALLINT NOT NULL,
    UsuarioId INT NULL,
    NomeUsuario VARCHAR(50) NULL,
    Entidade VARCHAR(64) NOT NULL,
    RegistroId BIGINT NOT NULL,
    Origem VARCHAR(80) NOT NULL,
    UsuarioBanco VARCHAR(288) NOT NULL,
    KEY IX_LogAuditoria_Data (OcorridoEm, Id),
    KEY IX_LogAuditoria_Registro (Entidade, RegistroId, OcorridoEm),
    KEY IX_LogAuditoria_Usuario (UsuarioId, OcorridoEm),
    CONSTRAINT FK_LogAuditoria_Tipo FOREIGN KEY (TipoEventoAuditoriaId) REFERENCES TipoEventoAuditoria(Id),
    CONSTRAINT FK_LogAuditoria_Usuario FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS DetalheLogAuditoria (
    Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    LogAuditoriaId BIGINT NOT NULL,
    Campo VARCHAR(64) NOT NULL,
    ValorAnterior TEXT NULL,
    ValorNovo TEXT NULL,
    UNIQUE KEY UQ_DetalheLogAuditoria_Campo (LogAuditoriaId, Campo),
    CONSTRAINT FK_DetalheLogAuditoria_Log FOREIGN KEY (LogAuditoriaId) REFERENCES LogAuditoria(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
INSERT INTO TipoEventoAuditoria (Id, Codigo, Nome) VALUES
    (1, 'CRIACAO', 'Criação'), (2, 'ALTERACAO', 'Alteração'), (3, 'EXCLUSAO', 'Exclusão')
ON DUPLICATE KEY UPDATE Codigo = VALUES(Codigo), Nome = VALUES(Nome);
INSERT INTO FuncaoParticipante (Nome)
SELECT s.Nome FROM (SELECT 'Participante' AS Nome UNION ALL SELECT 'Coordenador'
    UNION ALL SELECT 'Motorista' UNION ALL SELECT 'Apoio') s
WHERE NOT EXISTS (SELECT 1 FROM FuncaoParticipante f WHERE f.Nome = s.Nome COLLATE utf8mb4_unicode_ci);

-- O aplicativo identifica o autor em cada conexão. SQL externo sem contexto registra autor NULL.
-- Os triggers não cobrem alterações anteriores à instalação, SELECT, DDL ou ações do administrador.
DROP PROCEDURE IF EXISTS Figo_RegistrarAuditoria;
DELIMITER $$
CREATE PROCEDURE Figo_RegistrarAuditoria(
    IN p_entidade VARCHAR(64), IN p_registro BIGINT, IN p_tipo SMALLINT,
    IN p_antes JSON, IN p_depois JSON)
BEGIN
    DECLARE v_chaves JSON;
    DECLARE v_campo VARCHAR(64);
    DECLARE v_caminho VARCHAR(100);
    DECLARE v_antes JSON;
    DECLARE v_depois JSON;
    DECLARE v_texto_antes TEXT;
    DECLARE v_texto_depois TEXT;
    DECLARE v_i INT DEFAULT 0;
    DECLARE v_log BIGINT DEFAULT NULL;
    DECLARE v_usuario INT DEFAULT NULL;
    DECLARE v_nome VARCHAR(50) DEFAULT NULL;
    SET v_chaves = JSON_KEYS(COALESCE(p_depois, p_antes));
    SELECT MAX(Id), MAX(NomeUsuario) INTO v_usuario, v_nome FROM Usuarios WHERE Id = @figo_usuario_id;
    WHILE v_i < JSON_LENGTH(v_chaves) DO
        SET v_campo = JSON_UNQUOTE(JSON_EXTRACT(v_chaves, CONCAT('$[', v_i, ']')));
        SET v_caminho = CONCAT('$.', v_campo);
        SET v_antes = JSON_EXTRACT(p_antes, v_caminho);
        SET v_depois = JSON_EXTRACT(p_depois, v_caminho);
        -- Compara os valores reais ANTES de mascarar; alteração de CPF com mesmo final também gera log.
        IF NOT (v_antes <=> v_depois) THEN
            IF v_log IS NULL THEN
                INSERT INTO LogAuditoria (OcorridoEm, TipoEventoAuditoriaId, UsuarioId, NomeUsuario,
                    Entidade, RegistroId, Origem, UsuarioBanco)
                VALUES (UTC_TIMESTAMP(6), p_tipo, v_usuario, v_nome, p_entidade, p_registro,
                    COALESCE(NULLIF(LEFT(@figo_origem, 80), ''), 'SQL direto'), USER());
                SET v_log = LAST_INSERT_ID();
            END IF;
            SET v_texto_antes = IF(v_antes IS NULL OR JSON_TYPE(v_antes) = 'NULL', NULL, JSON_UNQUOTE(v_antes));
            SET v_texto_depois = IF(v_depois IS NULL OR JSON_TYPE(v_depois) = 'NULL', NULL, JSON_UNQUOTE(v_depois));
            IF (BINARY p_entidade = 'Participante' AND BINARY v_campo = 'Cpf')
                OR (BINARY p_entidade = 'Pagador' AND BINARY v_campo = 'Documento') THEN
                SET v_texto_antes = IF(v_texto_antes IS NULL, NULL, CONCAT('***', RIGHT(v_texto_antes, 2)));
                SET v_texto_depois = IF(v_texto_depois IS NULL, NULL, CONCAT('***', RIGHT(v_texto_depois, 2)));
            END IF;
            INSERT INTO DetalheLogAuditoria (LogAuditoriaId, Campo, ValorAnterior, ValorNovo)
            VALUES (v_log, v_campo, v_texto_antes, v_texto_depois);
        END IF;
        SET v_i = v_i + 1;
    END WHILE;
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Viagem_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Viagem_INSERT AFTER INSERT ON Viagem
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Viagem', NEW.Id, 1,
        NULL,
        JSON_OBJECT('Nome', NEW.Nome, 'Destino', NEW.Destino, 'DataInicio', NEW.DataInicio, 'DataFim', NEW.DataFim, 'QuantidadeParticipantes', NEW.QuantidadeParticipantes));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Viagem_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Viagem_UPDATE AFTER UPDATE ON Viagem
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Viagem', NEW.Id, 2,
        JSON_OBJECT('Nome', OLD.Nome, 'Destino', OLD.Destino, 'DataInicio', OLD.DataInicio, 'DataFim', OLD.DataFim, 'QuantidadeParticipantes', OLD.QuantidadeParticipantes),
        JSON_OBJECT('Nome', NEW.Nome, 'Destino', NEW.Destino, 'DataInicio', NEW.DataInicio, 'DataFim', NEW.DataFim, 'QuantidadeParticipantes', NEW.QuantidadeParticipantes));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Viagem_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Viagem_DELETE AFTER DELETE ON Viagem
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Viagem', OLD.Id, 3,
        JSON_OBJECT('Nome', OLD.Nome, 'Destino', OLD.Destino, 'DataInicio', OLD.DataInicio, 'DataFim', OLD.DataFim, 'QuantidadeParticipantes', OLD.QuantidadeParticipantes),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Receita_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Receita_INSERT AFTER INSERT ON Receita
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Receita', NEW.Id, 1,
        NULL,
        JSON_OBJECT('ViagemId', NEW.ViagemId, 'Descricao', NEW.Descricao, 'Valor', NEW.Valor, 'Data', NEW.Data, 'Origem', NEW.Origem, 'OrigemId', NEW.OrigemId, 'PagadorId', NEW.PagadorId, 'FormaPagamentoId', NEW.FormaPagamentoId, 'ContaFinanceiraId', NEW.ContaFinanceiraId, 'ReferenciaPagamento', NEW.ReferenciaPagamento));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Receita_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Receita_UPDATE AFTER UPDATE ON Receita
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Receita', NEW.Id, 2,
        JSON_OBJECT('ViagemId', OLD.ViagemId, 'Descricao', OLD.Descricao, 'Valor', OLD.Valor, 'Data', OLD.Data, 'Origem', OLD.Origem, 'OrigemId', OLD.OrigemId, 'PagadorId', OLD.PagadorId, 'FormaPagamentoId', OLD.FormaPagamentoId, 'ContaFinanceiraId', OLD.ContaFinanceiraId, 'ReferenciaPagamento', OLD.ReferenciaPagamento),
        JSON_OBJECT('ViagemId', NEW.ViagemId, 'Descricao', NEW.Descricao, 'Valor', NEW.Valor, 'Data', NEW.Data, 'Origem', NEW.Origem, 'OrigemId', NEW.OrigemId, 'PagadorId', NEW.PagadorId, 'FormaPagamentoId', NEW.FormaPagamentoId, 'ContaFinanceiraId', NEW.ContaFinanceiraId, 'ReferenciaPagamento', NEW.ReferenciaPagamento));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Receita_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Receita_DELETE AFTER DELETE ON Receita
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Receita', OLD.Id, 3,
        JSON_OBJECT('ViagemId', OLD.ViagemId, 'Descricao', OLD.Descricao, 'Valor', OLD.Valor, 'Data', OLD.Data, 'Origem', OLD.Origem, 'OrigemId', OLD.OrigemId, 'PagadorId', OLD.PagadorId, 'FormaPagamentoId', OLD.FormaPagamentoId, 'ContaFinanceiraId', OLD.ContaFinanceiraId, 'ReferenciaPagamento', OLD.ReferenciaPagamento),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Despesa_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Despesa_INSERT AFTER INSERT ON Despesa
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Despesa', NEW.Id, 1,
        NULL,
        JSON_OBJECT('ViagemId', NEW.ViagemId, 'Descricao', NEW.Descricao, 'Valor', NEW.Valor, 'Data', NEW.Data, 'Categoria', NEW.Categoria, 'CategoriaId', NEW.CategoriaId));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Despesa_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Despesa_UPDATE AFTER UPDATE ON Despesa
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Despesa', NEW.Id, 2,
        JSON_OBJECT('ViagemId', OLD.ViagemId, 'Descricao', OLD.Descricao, 'Valor', OLD.Valor, 'Data', OLD.Data, 'Categoria', OLD.Categoria, 'CategoriaId', OLD.CategoriaId),
        JSON_OBJECT('ViagemId', NEW.ViagemId, 'Descricao', NEW.Descricao, 'Valor', NEW.Valor, 'Data', NEW.Data, 'Categoria', NEW.Categoria, 'CategoriaId', NEW.CategoriaId));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Despesa_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Despesa_DELETE AFTER DELETE ON Despesa
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Despesa', OLD.Id, 3,
        JSON_OBJECT('ViagemId', OLD.ViagemId, 'Descricao', OLD.Descricao, 'Valor', OLD.Valor, 'Data', OLD.Data, 'Categoria', OLD.Categoria, 'CategoriaId', OLD.CategoriaId),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Usuarios_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Usuarios_INSERT AFTER INSERT ON Usuarios
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Usuarios', NEW.Id, 1,
        NULL,
        JSON_OBJECT('NomeUsuario', NEW.NomeUsuario, 'CriadoEm', NEW.CriadoEm, 'CredenciaisAlteradas', TRUE));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Usuarios_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Usuarios_UPDATE AFTER UPDATE ON Usuarios
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Usuarios', NEW.Id, 2,
        JSON_OBJECT('NomeUsuario', OLD.NomeUsuario, 'CriadoEm', OLD.CriadoEm, 'CredenciaisAlteradas', FALSE),
        JSON_OBJECT('NomeUsuario', NEW.NomeUsuario, 'CriadoEm', NEW.CriadoEm, 'CredenciaisAlteradas', NOT (OLD.SenhaHash <=> NEW.SenhaHash AND OLD.Salt <=> NEW.Salt AND OLD.Iteracoes <=> NEW.Iteracoes)));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Usuarios_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Usuarios_DELETE AFTER DELETE ON Usuarios
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Usuarios', OLD.Id, 3,
        JSON_OBJECT('NomeUsuario', OLD.NomeUsuario, 'CriadoEm', OLD.CriadoEm, 'CredenciaisAlteradas', TRUE),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Pagador_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Pagador_INSERT AFTER INSERT ON Pagador
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Pagador', NEW.Id, 1,
        NULL,
        JSON_OBJECT('Tipo', NEW.Tipo, 'Nome', NEW.Nome, 'Documento', NEW.Documento));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Pagador_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Pagador_UPDATE AFTER UPDATE ON Pagador
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Pagador', NEW.Id, 2,
        JSON_OBJECT('Tipo', OLD.Tipo, 'Nome', OLD.Nome, 'Documento', OLD.Documento),
        JSON_OBJECT('Tipo', NEW.Tipo, 'Nome', NEW.Nome, 'Documento', NEW.Documento));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Pagador_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Pagador_DELETE AFTER DELETE ON Pagador
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Pagador', OLD.Id, 3,
        JSON_OBJECT('Tipo', OLD.Tipo, 'Nome', OLD.Nome, 'Documento', OLD.Documento),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_OrigemReceita_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_OrigemReceita_INSERT AFTER INSERT ON OrigemReceita
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('OrigemReceita', NEW.Id, 1,
        NULL,
        JSON_OBJECT('Nome', NEW.Nome));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_OrigemReceita_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_OrigemReceita_UPDATE AFTER UPDATE ON OrigemReceita
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('OrigemReceita', NEW.Id, 2,
        JSON_OBJECT('Nome', OLD.Nome),
        JSON_OBJECT('Nome', NEW.Nome));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_OrigemReceita_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_OrigemReceita_DELETE AFTER DELETE ON OrigemReceita
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('OrigemReceita', OLD.Id, 3,
        JSON_OBJECT('Nome', OLD.Nome),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_FormaPagamento_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_FormaPagamento_INSERT AFTER INSERT ON FormaPagamento
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('FormaPagamento', NEW.Id, 1,
        NULL,
        JSON_OBJECT('Nome', NEW.Nome));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_FormaPagamento_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_FormaPagamento_UPDATE AFTER UPDATE ON FormaPagamento
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('FormaPagamento', NEW.Id, 2,
        JSON_OBJECT('Nome', OLD.Nome),
        JSON_OBJECT('Nome', NEW.Nome));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_FormaPagamento_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_FormaPagamento_DELETE AFTER DELETE ON FormaPagamento
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('FormaPagamento', OLD.Id, 3,
        JSON_OBJECT('Nome', OLD.Nome),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ContaFinanceira_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ContaFinanceira_INSERT AFTER INSERT ON ContaFinanceira
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('ContaFinanceira', NEW.Id, 1,
        NULL,
        JSON_OBJECT('Nome', NEW.Nome, 'Tipo', NEW.Tipo, 'Banco', NEW.Banco, 'Agencia', NEW.Agencia, 'Numero', NEW.Numero, 'Titular', NEW.Titular));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ContaFinanceira_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ContaFinanceira_UPDATE AFTER UPDATE ON ContaFinanceira
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('ContaFinanceira', NEW.Id, 2,
        JSON_OBJECT('Nome', OLD.Nome, 'Tipo', OLD.Tipo, 'Banco', OLD.Banco, 'Agencia', OLD.Agencia, 'Numero', OLD.Numero, 'Titular', OLD.Titular),
        JSON_OBJECT('Nome', NEW.Nome, 'Tipo', NEW.Tipo, 'Banco', NEW.Banco, 'Agencia', NEW.Agencia, 'Numero', NEW.Numero, 'Titular', NEW.Titular));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ContaFinanceira_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ContaFinanceira_DELETE AFTER DELETE ON ContaFinanceira
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('ContaFinanceira', OLD.Id, 3,
        JSON_OBJECT('Nome', OLD.Nome, 'Tipo', OLD.Tipo, 'Banco', OLD.Banco, 'Agencia', OLD.Agencia, 'Numero', OLD.Numero, 'Titular', OLD.Titular),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_CategoriaDespesa_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_CategoriaDespesa_INSERT AFTER INSERT ON CategoriaDespesa
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('CategoriaDespesa', NEW.Id, 1,
        NULL,
        JSON_OBJECT('Nome', NEW.Nome));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_CategoriaDespesa_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_CategoriaDespesa_UPDATE AFTER UPDATE ON CategoriaDespesa
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('CategoriaDespesa', NEW.Id, 2,
        JSON_OBJECT('Nome', OLD.Nome),
        JSON_OBJECT('Nome', NEW.Nome));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_CategoriaDespesa_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_CategoriaDespesa_DELETE AFTER DELETE ON CategoriaDespesa
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('CategoriaDespesa', OLD.Id, 3,
        JSON_OBJECT('Nome', OLD.Nome),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Participante_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Participante_INSERT AFTER INSERT ON Participante
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Participante', NEW.Id, 1,
        NULL,
        JSON_OBJECT('NomeCompleto', NEW.NomeCompleto, 'Cpf', NEW.Cpf));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Participante_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Participante_UPDATE AFTER UPDATE ON Participante
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Participante', NEW.Id, 2,
        JSON_OBJECT('NomeCompleto', OLD.NomeCompleto, 'Cpf', OLD.Cpf),
        JSON_OBJECT('NomeCompleto', NEW.NomeCompleto, 'Cpf', NEW.Cpf));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_Participante_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_Participante_DELETE AFTER DELETE ON Participante
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('Participante', OLD.Id, 3,
        JSON_OBJECT('NomeCompleto', OLD.NomeCompleto, 'Cpf', OLD.Cpf),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_FuncaoParticipante_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_FuncaoParticipante_INSERT AFTER INSERT ON FuncaoParticipante
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('FuncaoParticipante', NEW.Id, 1,
        NULL,
        JSON_OBJECT('Nome', NEW.Nome));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_FuncaoParticipante_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_FuncaoParticipante_UPDATE AFTER UPDATE ON FuncaoParticipante
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('FuncaoParticipante', NEW.Id, 2,
        JSON_OBJECT('Nome', OLD.Nome),
        JSON_OBJECT('Nome', NEW.Nome));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_FuncaoParticipante_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_FuncaoParticipante_DELETE AFTER DELETE ON FuncaoParticipante
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('FuncaoParticipante', OLD.Id, 3,
        JSON_OBJECT('Nome', OLD.Nome),
        NULL);
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ViagemParticipante_INSERT;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ViagemParticipante_INSERT AFTER INSERT ON ViagemParticipante
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('ViagemParticipante', NEW.Id, 1,
        NULL,
        JSON_OBJECT('ViagemId', NEW.ViagemId, 'ParticipanteId', NEW.ParticipanteId, 'Idade', NEW.Idade, 'FuncaoParticipanteId', NEW.FuncaoParticipanteId));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ViagemParticipante_UPDATE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ViagemParticipante_UPDATE AFTER UPDATE ON ViagemParticipante
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('ViagemParticipante', NEW.Id, 2,
        JSON_OBJECT('ViagemId', OLD.ViagemId, 'ParticipanteId', OLD.ParticipanteId, 'Idade', OLD.Idade, 'FuncaoParticipanteId', OLD.FuncaoParticipanteId),
        JSON_OBJECT('ViagemId', NEW.ViagemId, 'ParticipanteId', NEW.ParticipanteId, 'Idade', NEW.Idade, 'FuncaoParticipanteId', NEW.FuncaoParticipanteId));
END$$
DELIMITER ;

DROP TRIGGER IF EXISTS Figo_Audit_ViagemParticipante_DELETE;
DELIMITER $$
CREATE TRIGGER Figo_Audit_ViagemParticipante_DELETE AFTER DELETE ON ViagemParticipante
FOR EACH ROW BEGIN
    CALL Figo_RegistrarAuditoria('ViagemParticipante', OLD.Id, 3,
        JSON_OBJECT('ViagemId', OLD.ViagemId, 'ParticipanteId', OLD.ParticipanteId, 'Idade', OLD.Idade, 'FuncaoParticipanteId', OLD.FuncaoParticipanteId),
        NULL);
END$$
DELIMITER ;

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
