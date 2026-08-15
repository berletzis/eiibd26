/*
    add-regeneracion-procesada.sql
    ---------------------------------------------------------------------------
    ⚠️ DEPLOY-GATE: correr ESTO ANTES de desplegar el código.

    La columna se mapea como propiedad de las entidades `sintomas` y `tratamientos`, así
    que EF la incluye en el SELECT de CUALQUIER consulta a esas tablas — no solo en el
    batch. Si el código sale a producción sin la columna, revienta con "Invalid column
    name" toda la sección de síntomas y tratamientos (grillas, fichas públicas, PDF),
    no únicamente la actualización masiva. Primero la columna, después el deploy.

    Qué resuelve
    ------------
    Hasta ahora el re-proceso avanzaba por posición (skip/take) y el skip vivía SOLO en
    el navegador: recargar la página o caerse la sesión = volver al registro 0 y re-gastar
    llamadas a la IA. Con miles de tratamientos eso es inviable.

    La marca es persistente, así que el resume es inmune a recargas: se sella cuando el
    gate da un veredicto DEFINITIVO (Reconocido / NoReconocido / RevisionHumana) y NO se
    sella con GroundingNoDisponible (outage de la API), que queda pendiente para que la
    corrida siguiente lo reintente sola.

    Idempotente: se puede correr las veces que haga falta.
    Reset (para rehacer TODO tras mejorar el prompt): SQL/reset-regeneracion-procesada.sql
*/

SET NOCOUNT ON;

-- ===== 1. Columna en dbo.sintomas =====
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.sintomas')
                 AND name = 'RegeneracionProcesadaUtc')
BEGIN
    ALTER TABLE dbo.sintomas ADD RegeneracionProcesadaUtc DATETIME2 NULL;
    PRINT 'sintomas.RegeneracionProcesadaUtc creada.';
END
ELSE
    PRINT 'sintomas.RegeneracionProcesadaUtc ya existía — sin cambios.';
GO

-- ===== 2. Columna en dbo.tratamientos =====
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.tratamientos')
                 AND name = 'RegeneracionProcesadaUtc')
BEGIN
    ALTER TABLE dbo.tratamientos ADD RegeneracionProcesadaUtc DATETIME2 NULL;
    PRINT 'tratamientos.RegeneracionProcesadaUtc creada.';
END
ELSE
    PRINT 'tratamientos.RegeneracionProcesadaUtc ya existía — sin cambios.';
GO

/*
    ===== 3. Índices para el filtro de "siguientes pendientes" =====

    Filtrado sobre la marca: el batch pregunta siempre por IS NULL, y conforme la corrida
    avanza el índice se va vaciando solo. En tratamientos (miles de filas) es lo que evita
    un scan por cada sub-lote.

    Ojo al cargar este archivo con sqlcmd: los índices filtrados exigen -I (QUOTED_IDENTIFIER
    ON), y el archivo va en UTF-8 → usar también -f 65001 para no romper los acentos.
*/
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.sintomas')
                 AND name = 'IX_sintomas_RegeneracionPendiente')
BEGIN
    CREATE NONCLUSTERED INDEX IX_sintomas_RegeneracionPendiente
        ON dbo.sintomas (id)
        INCLUDE (ValidadoIA, ValidadoHumano, RevisionLimpiezaEstado)
        WHERE RegeneracionProcesadaUtc IS NULL AND Eliminado = 0;
    PRINT 'IX_sintomas_RegeneracionPendiente creado.';
END
ELSE
    PRINT 'IX_sintomas_RegeneracionPendiente ya existía — sin cambios.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.tratamientos')
                 AND name = 'IX_tratamientos_RegeneracionPendiente')
BEGIN
    CREATE NONCLUSTERED INDEX IX_tratamientos_RegeneracionPendiente
        ON dbo.tratamientos (id)
        INCLUDE (ValidadoIA, ValidadoHumano, RevisionLimpiezaEstado)
        WHERE RegeneracionProcesadaUtc IS NULL AND Eliminado = 0;
    PRINT 'IX_tratamientos_RegeneracionPendiente creado.';
END
ELSE
    PRINT 'IX_tratamientos_RegeneracionPendiente ya existía — sin cambios.';
GO

-- ===== 4. Verificación =====
SELECT
    tabla       = 'sintomas',
    total       = COUNT(*),
    pendientes  = SUM(CASE WHEN RegeneracionProcesadaUtc IS NULL THEN 1 ELSE 0 END),
    procesados  = SUM(CASE WHEN RegeneracionProcesadaUtc IS NOT NULL THEN 1 ELSE 0 END)
FROM dbo.sintomas WHERE Eliminado = 0
UNION ALL
SELECT
    'tratamientos',
    COUNT(*),
    SUM(CASE WHEN RegeneracionProcesadaUtc IS NULL THEN 1 ELSE 0 END),
    SUM(CASE WHEN RegeneracionProcesadaUtc IS NOT NULL THEN 1 ELSE 0 END)
FROM dbo.tratamientos WHERE Eliminado = 0;
