/*
    add-tratamientos-revision-limpieza.sql
    ---------------------------------------------------------------------------
    Triage de limpieza de tratamientos con NINA (3 vías).

    RevisionLimpiezaEstado:
        NULL = NoRevisado
        1    = Válido   (es una intervención terapéutica real, tenga o no relación con EII)
        2    = Basura   (no es una intervención terapéutica)
        3    = Dudoso   (ambiguo → cola de revisión humana, nunca auto-desactivar)

    OJO: el estado de limpieza (Eje 1: ¿es un tratamiento?) es INDEPENDIENTE del
    nivel de relación con EII (Eje 2). Lo no-EII pero real se conserva como Válido.

    Idempotente. Sin migración EF Core (política del proyecto).
*/

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.tratamientos') AND name = 'RevisionLimpiezaEstado')
BEGIN
    ALTER TABLE dbo.tratamientos ADD RevisionLimpiezaEstado TINYINT NULL;
    PRINT 'ADD  dbo.tratamientos.RevisionLimpiezaEstado';
END
ELSE PRINT 'SKIP dbo.tratamientos.RevisionLimpiezaEstado (ya existe)';

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.tratamientos') AND name = 'RevisionLimpiezaConfianza')
BEGIN
    ALTER TABLE dbo.tratamientos ADD RevisionLimpiezaConfianza DECIMAL(4,3) NULL;
    PRINT 'ADD  dbo.tratamientos.RevisionLimpiezaConfianza';
END
ELSE PRINT 'SKIP dbo.tratamientos.RevisionLimpiezaConfianza (ya existe)';

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.tratamientos') AND name = 'RevisionLimpiezaMotivo')
BEGIN
    ALTER TABLE dbo.tratamientos ADD RevisionLimpiezaMotivo NVARCHAR(400) NULL;
    PRINT 'ADD  dbo.tratamientos.RevisionLimpiezaMotivo';
END
ELSE PRINT 'SKIP dbo.tratamientos.RevisionLimpiezaMotivo (ya existe)';

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.tratamientos') AND name = 'RevisionLimpiezaFecha')
BEGIN
    ALTER TABLE dbo.tratamientos ADD RevisionLimpiezaFecha DATETIME2 NULL;
    PRINT 'ADD  dbo.tratamientos.RevisionLimpiezaFecha';
END
ELSE PRINT 'SKIP dbo.tratamientos.RevisionLimpiezaFecha (ya existe)';
GO

/*  Índice para el filtro "siguientes N no revisados":
    WHERE !Eliminado AND RevisionLimpiezaEstado IS NULL ORDER BY id            */
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.tratamientos')
                 AND name = 'IX_tratamientos_RevisionLimpiezaEstado')
BEGIN
    CREATE NONCLUSTERED INDEX IX_tratamientos_RevisionLimpiezaEstado
        ON dbo.tratamientos (RevisionLimpiezaEstado, Eliminado)
        INCLUDE (nombre);
    PRINT 'ADD  IX_tratamientos_RevisionLimpiezaEstado';
END
ELSE PRINT 'SKIP IX_tratamientos_RevisionLimpiezaEstado (ya existe)';
GO

/*  ---------------------------------------------------------------------------
    DESHACER EN BLOQUE lo desactivado por la IA (no ejecutar aquí; queda como
    referencia). Distingue "basura por IA" de un borrado manual del admin.
    ---------------------------------------------------------------------------
    UPDATE dbo.tratamientos
       SET Eliminado = 0, fechaEliminado = '1900-01-01', fechaModificado = GETDATE()
     WHERE RevisionLimpiezaEstado = 2 AND Eliminado = 1;
*/

SELECT name, system_type_name = TYPE_NAME(user_type_id), is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.tratamientos') AND name LIKE 'RevisionLimpieza%'
ORDER BY name;
