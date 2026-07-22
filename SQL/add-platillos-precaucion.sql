-- add-platillos-precaucion.sql
-- Anexo 5: nota de precaución de seguridad alimentaria. REUSA PlatNotaClinica + el candado;
-- no hay tabla ni texto fijo. Dos columnas:
--   1) PlatGrupo.RiesgoTipo  → flag de grupo de riesgo (NULL = sin riesgo). Su valor da CONTEXTO
--      a la IA (ej. 'marisco-crudo', 'carne-poco-cocida', 'sin-pasteurizar'); no es catálogo cerrado.
--   2) PlatNotaClinica.TipoNota → discrimina 'Tolerancia' (default: TODO lo existente) de 'Precaucion'.
--      Default en la BD para que las filas actuales queden como Tolerancia sin tocarlas.
-- Idempotente. Cambio de esquema por SQL directo (sin migraciones EF), como el resto del módulo.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.PlatGrupo') AND name = 'RiesgoTipo')
    ALTER TABLE dbo.PlatGrupo ADD RiesgoTipo NVARCHAR(30) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.PlatNotaClinica') AND name = 'TipoNota')
    ALTER TABLE dbo.PlatNotaClinica ADD TipoNota NVARCHAR(20) NOT NULL
        CONSTRAINT DF_PlatNotaClinica_TipoNota DEFAULT 'Tolerancia';
