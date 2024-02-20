-- Table definition for Account entity
CREATE TABLE IF NOT EXISTS Accounts (
    Id SERIAL PRIMARY KEY,
    Saldo NUMERIC NOT NULL,
    Limite NUMERIC NOT NULL
);

-- Table definition for Transaction entity
CREATE TABLE IF NOT EXISTS Transactions (
    Id SERIAL PRIMARY KEY,
    AccountId INT NOT NULL,
    Descricao VARCHAR(10),
    Tipo CHAR(1) NOT NULL,
    Valor NUMERIC NOT NULL,
    RealizadaEm TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
);