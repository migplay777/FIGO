-- Somente para o banco isolado figo_test do CI. Não executar no banco de uso.
CREATE TABLE Viagem (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(255) NOT NULL, Destino VARCHAR(255) NOT NULL,
    DataInicio DATE NOT NULL, DataFim DATE NOT NULL, QuantidadeParticipantes INT NOT NULL
) ENGINE=InnoDB;
CREATE TABLE Receita (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ViagemId INT NOT NULL, Descricao VARCHAR(255) NOT NULL, Valor DECIMAL(12,2) NOT NULL,
    Data DATE NOT NULL, Origem VARCHAR(255) NOT NULL,
    FOREIGN KEY (ViagemId) REFERENCES Viagem(Id)
) ENGINE=InnoDB;
CREATE TABLE Despesa (
    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ViagemId INT NOT NULL, Descricao VARCHAR(255) NOT NULL, Valor DECIMAL(12,2) NOT NULL,
    Data DATE NOT NULL, Categoria VARCHAR(255) NOT NULL,
    FOREIGN KEY (ViagemId) REFERENCES Viagem(Id)
) ENGINE=InnoDB;
CREATE VIEW vw_ResumoViagem AS
SELECT v.Id AS ViagemId, v.Nome, v.Destino, v.QuantidadeParticipantes,
    COALESCE(r.Total, 0) AS TotalReceitas, COALESCE(d.Total, 0) AS TotalDespesas,
    COALESCE(r.Total, 0) - COALESCE(d.Total, 0) AS Saldo
FROM Viagem v
LEFT JOIN (SELECT ViagemId, SUM(Valor) AS Total FROM Receita GROUP BY ViagemId) r ON r.ViagemId = v.Id
LEFT JOIN (SELECT ViagemId, SUM(Valor) AS Total FROM Despesa GROUP BY ViagemId) d ON d.ViagemId = v.Id;
INSERT INTO Viagem (Nome, Destino, DataInicio, DataFim, QuantidadeParticipantes)
VALUES ('Viagem legada de teste', 'Campinas', '2026-10-20', '2026-10-21', 5);
INSERT INTO Receita (ViagemId, Descricao, Valor, Data, Origem)
VALUES (1, 'Receita antiga', 100, '2026-10-20', 'Igreja antiga');
INSERT INTO Despesa (ViagemId, Descricao, Valor, Data, Categoria)
VALUES (1, 'Despesa antiga', 20, '2026-10-20', 'Transporte legado');
