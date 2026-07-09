/* =====================================================================
   Motor de Cobertura - Fase 2 (Embeddings): almacenamiento del vector
   ---------------------------------------------------------------------
   Migracion firma-por-conteo -> embeddings Voyage (voyage-4-large, 1024 dims).

   SQL Server 2019 Express: NO existe el tipo VECTOR nativo (llego en SQL
   Server 2025). Se guarda el vector como JSON en NVARCHAR(MAX) y el coseno
   se calcula en memoria (mismo patron que la firma).

   Este script:
     1. Columnas Embedding* en dbo.contenidos   (contenido PROPIO, lo firma el Web)
     2. Columnas Embedding* en dbo.ScrapedPage   (externos, los firma el Worker)
     3. Tabla dbo.CoberturaSimilitudEmbedding    (coexiste con CoberturaSimilitud)

   IDEMPOTENTE. Lo ejecuta el USUARIO. Claude Code NO corre DDL en produccion.
   NO toca CoberturaSimilitud, contenidos.Firma ni ScrapedPage.Firma (motor de
   firma queda en STANDBY, intacto).
   ===================================================================== */

/* ---------------------------------------------------------------------
   1. dbo.contenidos  (Web - contenido propio)
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.contenidos') AND name = 'Embedding')
    ALTER TABLE dbo.contenidos ADD Embedding NVARCHAR(MAX) NULL;   -- JSON: [f1,f2,...,f1024]

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.contenidos') AND name = 'EmbeddingModelo')
    ALTER TABLE dbo.contenidos ADD EmbeddingModelo NVARCHAR(40) NULL;   -- p.ej. 'voyage-4-large'

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.contenidos') AND name = 'EmbeddingCalculadoEn')
    ALTER TABLE dbo.contenidos ADD EmbeddingCalculadoEn DATETIME NULL;

/* ---------------------------------------------------------------------
   2. dbo.ScrapedPage  (Worker - externos; solo se guarda el vector, nunca el texto)
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.ScrapedPage') AND name = 'Embedding')
    ALTER TABLE dbo.ScrapedPage ADD Embedding NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.ScrapedPage') AND name = 'EmbeddingModelo')
    ALTER TABLE dbo.ScrapedPage ADD EmbeddingModelo NVARCHAR(40) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.ScrapedPage') AND name = 'EmbeddingCalculadoEn')
    ALTER TABLE dbo.ScrapedPage ADD EmbeddingCalculadoEn DATETIME NULL;

/* ---------------------------------------------------------------------
   3. dbo.CoberturaSimilitudEmbedding  (tabla nueva, coexiste con CoberturaSimilitud)
      Misma forma que la de firma:
        - AId: siempre un propio (contenidos.Id)
        - BId: propio (contenidos.Id) o externo (ScrapedPage.ScrapedPageId)
        - TipoPar: 1 = propio-propio, 2 = propio-externo
      Score con mas resolucion (coseno de embedding es mas fino que el de firma).
      Sin FKs (BId apunta a dos tablas segun TipoPar).
   --------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables
               WHERE name = 'CoberturaSimilitudEmbedding' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.CoberturaSimilitudEmbedding
    (
        Id            INT IDENTITY(1,1) NOT NULL
                      CONSTRAINT PK_CoberturaSimilitudEmbedding PRIMARY KEY,
        AId           INT            NOT NULL,   -- propio (contenidos.Id)
        BId           INT            NOT NULL,   -- propio (contenidos.Id) o externo (ScrapedPageId)
        TipoPar       TINYINT        NOT NULL,   -- 1=propio-propio, 2=propio-externo
        Score         DECIMAL(6,5)   NOT NULL,   -- coseno de embedding (-1..1; en la practica 0..1)
        AEmbEn        DATETIME       NULL,        -- timestamps de embedding (incremental)
        BEmbEn        DATETIME       NULL,
        CalculatedAt  DATETIME       NOT NULL
    );
END;

-- Indice unico del par (idempotente)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CobSimEmb_Par'
               AND object_id = OBJECT_ID('dbo.CoberturaSimilitudEmbedding'))
    CREATE UNIQUE INDEX UX_CobSimEmb_Par
        ON dbo.CoberturaSimilitudEmbedding (AId, BId, TipoPar);

-- Indice de apoyo para el top por score
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CobSimEmb_Score'
               AND object_id = OBJECT_ID('dbo.CoberturaSimilitudEmbedding'))
    CREATE INDEX IX_CobSimEmb_Score
        ON dbo.CoberturaSimilitudEmbedding (Score DESC);

/* ---------------------------------------------------------------------
   Verificacion (por metadata: NO referencia las columnas nuevas como
   columnas ligadas, asi el script compila en un solo batch sin GO y
   funciona en cualquier cliente -SSMS, sqlcmd- corriendolo una sola vez).
   Esperado: 6 filas (3 columnas x 2 tablas) + la tabla nueva presente.
   --------------------------------------------------------------------- */
SELECT t.name AS Tabla, c.name AS Columna, ty.name AS Tipo
FROM sys.columns c
JOIN sys.tables  t  ON t.object_id  = c.object_id
JOIN sys.types   ty ON ty.user_type_id = c.user_type_id
WHERE t.name IN ('contenidos', 'ScrapedPage')
  AND c.name IN ('Embedding', 'EmbeddingModelo', 'EmbeddingCalculadoEn')
ORDER BY t.name, c.name;

SELECT COUNT(*) AS ParesEmbeddingGuardados FROM dbo.CoberturaSimilitudEmbedding;
