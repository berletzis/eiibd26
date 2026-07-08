/* =====================================================================
   Motor de Cobertura — Fase 3: tabla de similitud (propia del Web)
   ---------------------------------------------------------------------
   Guarda pares y su similitud de coseno:
     - AId: siempre un propio (contenidos.Id)
     - BId: propio (contenidos.Id) o externo (ScrapedPage.ScrapedPageId)
     - TipoPar: 1 = propio-propio, 2 = propio-externo
   Sin FKs (BId apunta a dos tablas distintas segun TipoPar).

   IDEMPOTENTE. Ejecuta el USUARIO. Claude Code NO corre DDL en produccion.
   Deja intacta ArticleSimilarity del Worker.
   ===================================================================== */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CoberturaSimilitud' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.CoberturaSimilitud
    (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CoberturaSimilitud PRIMARY KEY,
        AId           INT            NOT NULL,   -- propio (contenidos.Id)
        BId           INT            NOT NULL,   -- propio (contenidos.Id) o externo (ScrapedPageId)
        TipoPar       TINYINT        NOT NULL,   -- 1=propio-propio, 2=propio-externo
        Score         DECIMAL(5,4)   NOT NULL,   -- coseno 0..1
        AFirmaEn      DATETIME       NULL,       -- timestamps de firma (incremental)
        BFirmaEn      DATETIME       NULL,
        CalculatedAt  DATETIME       NOT NULL
    );
END;

-- Indice unico del par (idempotente)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CoberturaSimilitud_Par'
               AND object_id = OBJECT_ID('dbo.CoberturaSimilitud'))
    CREATE UNIQUE INDEX UX_CoberturaSimilitud_Par
        ON dbo.CoberturaSimilitud (AId, BId, TipoPar);

-- Indice de apoyo para el top por score
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CoberturaSimilitud_Score'
               AND object_id = OBJECT_ID('dbo.CoberturaSimilitud'))
    CREATE INDEX IX_CoberturaSimilitud_Score
        ON dbo.CoberturaSimilitud (Score DESC);

SELECT COUNT(*) AS ParesGuardados FROM dbo.CoberturaSimilitud;
