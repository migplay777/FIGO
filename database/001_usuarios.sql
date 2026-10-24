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
