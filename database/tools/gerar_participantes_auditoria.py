"""Gera os dois SQL distribuídos. Execute da raiz: python3 database/tools/gerar_participantes_auditoria.py."""
from pathlib import Path

root = Path(__file__).resolve().parents[2]
ddl = r"""-- FIGO | 003 | Participantes opcionais e auditoria | MySQL 8.0.16+ / 8.4
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
"""

# Lista explícita: não serializar linhas inteiras ou colunas de credenciais.
fields = {
    'Viagem': 'Nome Destino DataInicio DataFim QuantidadeParticipantes',
    'Receita': 'ViagemId Descricao Valor Data Origem OrigemId PagadorId FormaPagamentoId ContaFinanceiraId ReferenciaPagamento',
    'Despesa': 'ViagemId Descricao Valor Data Categoria CategoriaId',
    'Usuarios': 'NomeUsuario CriadoEm',
    'Pagador': 'Tipo Nome Documento',
    'OrigemReceita': 'Nome', 'FormaPagamento': 'Nome',
    'ContaFinanceira': 'Nome Tipo Banco Agencia Numero Titular',
    'CategoriaDespesa': 'Nome', 'Participante': 'NomeCompleto Cpf',
    'FuncaoParticipante': 'Nome',
    'ViagemParticipante': 'ViagemId ParticipanteId Idade FuncaoParticipanteId',
}

def obj(table, prefix, operation):
    args = [f"'{f}', {prefix}.{f}" for f in fields[table].split()]
    if table == 'Usuarios':
        changed = 'TRUE'
        if operation == 'UPDATE':
            changed = 'FALSE' if prefix == 'OLD' else 'NOT (OLD.SenhaHash <=> NEW.SenhaHash AND OLD.Salt <=> NEW.Salt AND OLD.Iteracoes <=> NEW.Iteracoes)'
        args.append(f"'CredenciaisAlteradas', {changed}")
    return 'JSON_OBJECT(' + ', '.join(args) + ')'

for table in fields:
    for operation, code in [('INSERT', 1), ('UPDATE', 2), ('DELETE', 3)]:
        name = f'Figo_Audit_{table}_{operation}'
        before = obj(table, 'OLD', operation) if operation != 'INSERT' else 'NULL'
        after = obj(table, 'NEW', operation) if operation != 'DELETE' else 'NULL'
        prefix = 'OLD' if operation == 'DELETE' else 'NEW'
        ddl += f"\nDROP TRIGGER IF EXISTS {name};\nDELIMITER $$\nCREATE TRIGGER {name} AFTER {operation} ON {table}\nFOR EACH ROW BEGIN\n    CALL Figo_RegistrarAuditoria('{table}', {prefix}.Id, {code},\n        {before},\n        {after});\nEND$$\nDELIMITER ;\n"

(root / 'database/003_participantes_auditoria.sql').write_text(ddl, encoding='utf-8')
base = (root / 'database/tests/base_legada.sql').read_text(encoding='utf-8').split('INSERT INTO Viagem')[0]
base = '\n'.join(base.splitlines()[1:]).replace('CREATE TABLE ', 'CREATE TABLE IF NOT EXISTS ').replace('CREATE VIEW ', 'CREATE OR REPLACE VIEW ')
full = "-- FIGO | Instalação completa vazia | MySQL 8.0.16+ / 8.4\n-- Para banco existente, prefira as migrações individuais; consulte docs/PARTICIPANTES_AUDITORIA.md.\nCREATE DATABASE IF NOT EXISTS figo CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;\nUSE figo;\n" + base
for migration in ['001_usuarios.sql', '002_cadastros_financeiros.sql', '003_participantes_auditoria.sql']:
    full += '\n' + (root / 'database' / migration).read_text(encoding='utf-8')
(root / 'database/000_instalacao_completa.sql').write_text(full, encoding='utf-8')
print('Gerados SQL 003 e 000: 6 novas tabelas, 36 triggers, total de 15 tabelas.')
