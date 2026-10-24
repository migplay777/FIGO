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
