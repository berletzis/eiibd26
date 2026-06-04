-- Short-URL F1: Tablas ShortUrls + ShortUrlClicks
-- Ejecutar directo en SQL Server. Idempotente.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShortUrls')
BEGIN
    CREATE TABLE ShortUrls (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Codigo      NVARCHAR(16) NOT NULL,
        UrlDestino  NVARCHAR(2000) NOT NULL,
        ClickCount  INT NOT NULL DEFAULT 0,
        Origen      NVARCHAR(50) NULL,
        FechaCreado DATETIME NOT NULL DEFAULT GETUTCDATE(),
        Eliminado   BIT NOT NULL DEFAULT 0,
        CONSTRAINT UX_ShortUrls_Codigo UNIQUE (Codigo)
    );
    CREATE INDEX IX_ShortUrls_Codigo ON ShortUrls(Codigo) WHERE Eliminado = 0;
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShortUrlClicks')
BEGIN
    CREATE TABLE ShortUrlClicks (
        Id         BIGINT IDENTITY(1,1) PRIMARY KEY,
        ShortUrlId INT NOT NULL,
        FechaClick DATETIME NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ShortUrlClicks_ShortUrl FOREIGN KEY (ShortUrlId) REFERENCES ShortUrls(Id)
    );
    CREATE INDEX IX_ShortUrlClicks_ShortUrlId ON ShortUrlClicks(ShortUrlId);
END
