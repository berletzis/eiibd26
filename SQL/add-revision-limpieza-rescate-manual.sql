/*
    add-revision-limpieza-rescate-manual.sql
    ---------------------------------------------------------------------------
    Flag explícito de RESCATE MANUAL para el triage de limpieza.

    PROBLEMA QUE RESUELVE
      Los rescates manuales (registros que un humano sacó del cubo Basura) se venían
      marcando escribiendo a mano "Rescate manual (Berletzis)" dentro de
      RevisionLimpiezaMotivo. Ese texto es una CONVENCIÓN: ningún código lo escribe ni
      lo lee, así que basta una falta de ortografía o un re-sellado de la IA para que el
      rescate se pierda y el registro se vuelva a clasificar desde cero.

    ALCANCE
      Columna SOLO de datos: NO se mapea en EF (no hay propiedad en los modelos), así que
      NO es un deploy-gate. La usan únicamente los scripts de mantenimiento — hoy,
      2026-08-12-reset-triage-para-regeneracion.sql, para no pisar rescates.

    Idempotente. Sin migración EF Core (política del proyecto).
*/

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.tratamientos') AND name = 'RevisionLimpiezaRescateManual')
BEGIN
    ALTER TABLE dbo.tratamientos ADD RevisionLimpiezaRescateManual BIT NOT NULL CONSTRAINT DF_tratamientos_RescateManual DEFAULT(0);
    PRINT 'ADD  dbo.tratamientos.RevisionLimpiezaRescateManual';
END
ELSE PRINT 'SKIP dbo.tratamientos.RevisionLimpiezaRescateManual (ya existe)';

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.sintomas') AND name = 'RevisionLimpiezaRescateManual')
BEGIN
    ALTER TABLE dbo.sintomas ADD RevisionLimpiezaRescateManual BIT NOT NULL CONSTRAINT DF_sintomas_RescateManual DEFAULT(0);
    PRINT 'ADD  dbo.sintomas.RevisionLimpiezaRescateManual';
END
ELSE PRINT 'SKIP dbo.sintomas.RevisionLimpiezaRescateManual (ya existe)';
GO

/*  ---------------------------------------------------------------------------
    SELLADO ÚNICO de los rescates que ya existen.

    Es la ÚNICA vez que se usa el LIKE sobre el motivo: es la única huella que dejaron
    los rescates viejos. De aquí en adelante manda la columna.

    REVISAR LA MUESTRA ANTES DE CORRER EL UPDATE — el patrón puede traer falsos
    positivos si alguien escribió el texto de otra forma.
    --------------------------------------------------------------------------- */

-- 1) Qué se va a marcar (revisar a ojo)
SELECT 'tratamientos' AS Tabla, id, nombre, RevisionLimpiezaEstado, RevisionLimpiezaMotivo
FROM dbo.tratamientos
WHERE RevisionLimpiezaMotivo LIKE '%Rescate manual%'
UNION ALL
SELECT 'sintomas', id, nombre, RevisionLimpiezaEstado, RevisionLimpiezaMotivo
FROM dbo.sintomas
WHERE RevisionLimpiezaMotivo LIKE '%Rescate manual%'
ORDER BY Tabla, id;

-- 2) Aplicar (descomentar tras revisar la muestra de arriba)
/*
BEGIN TRANSACTION;

UPDATE dbo.tratamientos
   SET RevisionLimpiezaRescateManual = 1
 WHERE RevisionLimpiezaMotivo LIKE '%Rescate manual%'
   AND RevisionLimpiezaRescateManual = 0;
PRINT N'Tratamientos sellados como rescate manual: ' + CAST(@@ROWCOUNT AS nvarchar(10));

UPDATE dbo.sintomas
   SET RevisionLimpiezaRescateManual = 1
 WHERE RevisionLimpiezaMotivo LIKE '%Rescate manual%'
   AND RevisionLimpiezaRescateManual = 0;
PRINT N'Síntomas sellados como rescate manual: ' + CAST(@@ROWCOUNT AS nvarchar(10));

COMMIT TRANSACTION;
*/

-- 3) Cómo marcar un rescate a mano de aquí en adelante:
--    UPDATE dbo.tratamientos
--       SET RevisionLimpiezaRescateManual = 1,
--           RevisionLimpiezaEstado = 1,
--           RevisionLimpiezaMotivo = 'Rescate manual: <por qué>'
--     WHERE id = <id>;
