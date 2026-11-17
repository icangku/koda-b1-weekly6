IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE TABLE [PaymentMethods] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ProviderName] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_PaymentMethods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] bigint NOT NULL IDENTITY,
        [Username] nvarchar(50) NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [PasswordHash] nvarchar(500) NULL,
        [Status] varchar(20) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Users_Status] CHECK ([Status] IN ('ACTIVE', 'SUSPENDED'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE TABLE [Wallets] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NOT NULL,
        [WalletNumber] nvarchar(30) NOT NULL,
        [Balance] decimal(18,2) NOT NULL,
        [Status] varchar(20) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Wallets] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Wallets_Balance] CHECK ([Balance] >= 0),
        CONSTRAINT [CK_Wallets_Status] CHECK ([Status] IN ('ACTIVE', 'FROZEN', 'CLOSED')),
        CONSTRAINT [FK_Wallets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE TABLE [Transactions] (
        [Id] bigint NOT NULL IDENTITY,
        [ReferenceNo] nvarchar(50) NOT NULL,
        [SenderWalletId] bigint NULL,
        [ReceiverWalletId] bigint NOT NULL,
        [Type] varchar(20) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] varchar(20) NOT NULL,
        [Description] nvarchar(255) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Transactions_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_Transactions_Parties] CHECK (([Type] = 'TRANSFER' AND [SenderWalletId] IS NOT NULL AND [SenderWalletId] <> [ReceiverWalletId]) OR ([Type] = 'TOPUP' AND [SenderWalletId] IS NULL)),
        CONSTRAINT [CK_Transactions_Status] CHECK ([Status] IN ('PENDING','SUCCESS','FAILED')),
        CONSTRAINT [CK_Transactions_Type] CHECK ([Type] IN ('TOPUP','TRANSFER')),
        CONSTRAINT [FK_Transactions_Wallets_ReceiverWalletId] FOREIGN KEY ([ReceiverWalletId]) REFERENCES [Wallets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Transactions_Wallets_SenderWalletId] FOREIGN KEY ([SenderWalletId]) REFERENCES [Wallets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] bigint NULL,
        [TransactionId] bigint NULL,
        [Action] nvarchar(60) NOT NULL,
        [EntityType] nvarchar(60) NOT NULL,
        [EntityId] nvarchar(60) NULL,
        [Outcome] varchar(20) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CorrelationId] nvarchar(100) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AuditLogs_Outcome] CHECK ([Outcome] IN ('SUCCESS','REJECTED','FAILED')),
        CONSTRAINT [FK_AuditLogs_Transactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [Transactions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditLogs_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE TABLE [TopUpRequests] (
        [Id] bigint NOT NULL IDENTITY,
        [TransactionId] bigint NOT NULL,
        [WalletId] bigint NOT NULL,
        [PaymentMethodId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Status] varchar(20) NOT NULL,
        [ProviderReference] nvarchar(100) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ExpiredAtUtc] datetime2 NULL,
        [PaidAtUtc] datetime2 NULL,
        CONSTRAINT [PK_TopUpRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TopUpRequests_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_TopUpRequests_Status] CHECK ([Status] IN ('PENDING','PAID','FAILED','EXPIRED')),
        CONSTRAINT [FK_TopUpRequests_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [PaymentMethods] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TopUpRequests_Transactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [Transactions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TopUpRequests_Wallets_WalletId] FOREIGN KEY ([WalletId]) REFERENCES [Wallets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE TABLE [WalletMutations] (
        [Id] bigint NOT NULL IDENTITY,
        [TransactionId] bigint NOT NULL,
        [WalletId] bigint NOT NULL,
        [Direction] varchar(3) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [BalanceBefore] decimal(18,2) NOT NULL,
        [BalanceAfter] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_WalletMutations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WalletMutations_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_WalletMutations_Balance] CHECK (([Direction] = 'IN' AND [BalanceAfter] = [BalanceBefore] + [Amount]) OR ([Direction] = 'OUT' AND [BalanceAfter] = [BalanceBefore] - [Amount])),
        CONSTRAINT [CK_WalletMutations_Direction] CHECK ([Direction] IN ('IN','OUT')),
        CONSTRAINT [FK_WalletMutations_Transactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [Transactions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WalletMutations_Wallets_WalletId] FOREIGN KEY ([WalletId]) REFERENCES [Wallets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ActorUserId_CreatedAtUtc] ON [AuditLogs] ([ActorUserId], [CreatedAtUtc] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TransactionId] ON [AuditLogs] ([TransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentMethods_Code] ON [PaymentMethods] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TopUpRequests_PaymentMethodId] ON [TopUpRequests] ([PaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TopUpRequests_TransactionId] ON [TopUpRequests] ([TransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TopUpRequests_WalletId] ON [TopUpRequests] ([WalletId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_ReceiverWalletId_CreatedAtUtc] ON [Transactions] ([ReceiverWalletId], [CreatedAtUtc] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Transactions_ReferenceNo] ON [Transactions] ([ReferenceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_SenderWalletId_CreatedAtUtc] ON [Transactions] ([SenderWalletId], [CreatedAtUtc] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Phone] ON [Users] ([Phone]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WalletMutations_TransactionId_WalletId] ON [WalletMutations] ([TransactionId], [WalletId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WalletMutations_WalletId_CreatedAtUtc_Id] ON [WalletMutations] ([WalletId], [CreatedAtUtc] DESC, [Id] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Wallets_UserId] ON [Wallets] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Wallets_WalletNumber] ON [Wallets] ([WalletNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115161753_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115161753_InitialCreate', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115163952_FixColumnNames'
)
BEGIN
    EXEC sp_rename N'[Transactions].[UpdatedAtUtc]', N'CompletedAtUtc', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115163952_FixColumnNames'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115163952_FixColumnNames', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117154515_RenameTopUpExpiresAtUtc'
)
BEGIN
    EXEC sp_rename N'[TopUpRequests].[ExpiredAtUtc]', N'ExpiresAtUtc', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117154515_RenameTopUpExpiresAtUtc'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261117154515_RenameTopUpExpiresAtUtc', N'10.0.15');
END;

COMMIT;
GO

