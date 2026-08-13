/*
    add-sintomas-revision-limpieza.sql
    ---------------------------------------------------------------------------
    Triage de limpieza de síntomas con NINA (3 vías). Espejo del de tratamientos
    (ver add-tratamientos-revision-limpieza.sql).

    RevisionLimpiezaEstado:
        NULL = NoRevisado
        1    = Válido   (manifestación clínica real, tenga o no relación con EII)
        2    = Basura   (ruido, o un no-síntoma mal capturado como síntoma)
        3    = Dudoso   (ambiguo → cola de revisión humana, nunca auto-desactivar)

    OJO: el estado de limpieza (Eje 1: ¿es un síntoma?) es INDEPENDIENTE del nivel
    de relación con EII (Eje 2). Lo no-EII pero real se conserva como Válido.

    Idempotente. Sin migración EF Core (política del proyecto).
*/

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.sintomas') AND name = 'RevisionLimpiezaEstado')
BEGIN
    ALTER TABLE dbo.sintomas ADD RevisionLimpiezaEstado TINYINT NULL;
    PRINT 'ADD  dbo.sintomas.RevisionLimpiezaEstado';
END
ELSE PRINT 'SKIP dbo.sintomas.RevisionLimpiezaEstado (ya existe)';

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.sintomas') AND name = 'RevisionLimpiezaConfianza')
BEGIN
    ALTER TABLE dbo.sintomas ADD RevisionLimpiezaConfianza DECIMAL(4,3) NULL;
    PRINT 'ADD  dbo.sintomas.RevisionLimpiezaConfianza';
END
ELSE PRINT 'SKIP dbo.sintomas.RevisionLimpiezaConfianza (ya existe)';

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.sintomas') AND name = 'RevisionLimpiezaMotivo')
BEGIN
    ALTER TABLE dbo.sintomas ADD RevisionLimpiezaMotivo NVARCHAR(400) NULL;
    PRINT 'ADD  dbo.sintomas.RevisionLimpiezaMotivo';
END
ELSE PRINT 'SKIP dbo.sintomas.RevisionLimpiezaMotivo (ya existe)';

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.sintomas') AND name = 'RevisionLimpiezaFecha')
BEGIN
    ALTER TABLE dbo.sintomas ADD RevisionLimpiezaFecha DATETIME2 NULL;
    PRINT 'ADD  dbo.sintomas.RevisionLimpiezaFecha';
END
ELSE PRINT 'SKIP dbo.sintomas.RevisionLimpiezaFecha (ya existe)';
GO

/*  Índice para el filtro "siguientes N no revisados":
    WHERE !Eliminado AND RevisionLimpiezaEstado IS NULL ORDER BY id            */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.sintomas')
                 AND name = 'IX_sintomas_RevisionLimpiezaEstado')
BEGIN
    CREATE NONCLUSTERED INDEX IX_sintomas_RevisionLimpiezaEstado
        ON dbo.sintomas (RevisionLimpiezaEstado, Eliminado)
        INCLUDE (nombre);
    PRINT 'ADD  IX_sintomas_RevisionLimpiezaEstado';
END
ELSE PRINT 'SKIP IX_sintomas_RevisionLimpiezaEstado (ya existe)';
GO

/*  ---------------------------------------------------------------------------
    DESHACER EN BLOQUE lo desactivado por la IA (no ejecutar aquí; queda como
    referencia). Distingue "basura por IA" de un borrado manual del admin.
    OJO: hay que revertir TAMBIÉN GlossaryTerm.Activo — ver el bloque UNDO de
    2026-08-12-glosario-sincronizar-activo-sintomas.sql.
    ---------------------------------------------------------------------------
    UPDATE dbo.sintomas
       SET Eliminado = 0, fechaEliminado = '1900-01-01', fechaModificado = GETDATE()
     WHERE RevisionLimpiezaEstado = 2 AND Eliminado = 1;
*/

SELECT name, system_type_name = TYPE_NAME(user_type_id), is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.sintomas') AND name LIKE 'RevisionLimpieza%'
ORDER BY name;
