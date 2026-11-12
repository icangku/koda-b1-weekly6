-- 1. Tabel Users
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Phone NVARCHAR(20) NOT NULL
);

-- 2. Tabel Wallets
CREATE TABLE Wallets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,
    WalletNumber NVARCHAR(20) NOT NULL UNIQUE,
    Balance DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT FK_Wallets_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- 3. Tabel Transactions
CREATE TABLE Transactions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SenderWalletId INT NULL,
    ReceiverWalletId INT NOT NULL,
    Type NVARCHAR(20) NOT NULL, -- 'TopUp' atau 'Transfer'
    Amount DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Transactions_Sender FOREIGN KEY (SenderWalletId) REFERENCES Wallets(Id),
    CONSTRAINT FK_Transactions_Receiver FOREIGN KEY (ReceiverWalletId) REFERENCES Wallets(Id)
);

-- Indexing
CREATE INDEX IX_Wallets_UserId ON Wallets(UserId);
CREATE INDEX IX_Transactions_Sender ON Transactions(SenderWalletId);
CREATE INDEX IX_Transactions_Receiver ON Transactions(ReceiverWalletId);

-- Seed Data (3 User Awal)
INSERT INTO Users (Name, Email, Phone) VALUES 
('Andi Prasetyo', 'andi@example.com', '081234567890'),
('Budi Santoso', 'budi@example.com', '081234567891'),
('Citra Dewi', 'citra@example.com', '081234567892');

INSERT INTO Wallets (UserId, WalletNumber, Balance) VALUES 
(1, 'WAL001', 0.00),
(2, 'WAL002', 0.00),
(3, 'WAL003', 0.00);