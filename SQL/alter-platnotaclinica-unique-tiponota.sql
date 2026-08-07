/* =============================================================================
   Recrear UQ_PlatNotaClinica_Destino incluyendo TipoNota.

   PROBLEMA
   El constraint era UNIQUE (TipoDestino, DestinoId) — sin TipoNota. Pero la app
   trata la nota como una por (destino, TIPO): PlatNotaAdminService busca e
   inserta por la terna en GuardarBorradorAsync, PublicarAsync y DespublicarAsync,
   y es el unico escritor de la tabla. Resultado: una nota de Precaucion de
   seguridad era IMPOSIBLE de guardar en cualquier grupo o ingrediente que ya
   tuviera nota de Tolerancia. Los 0 registros de Precaucion en produccion no
   eran por falta de escritura: no se podian guardar donde tienen sentido.

   POR QUE ES SEGURO
   La terna es MENOS restrictiva que el par: toda fila que cumple el par cumple
   la terna, asi que recrearlo no puede fallar por datos duplicados. No agrega ni
   quita columnas, asi que no aplica la regla de orden ADD-antes / DROP-despues:
   se puede correr en cualquier momento, antes o despues del despliegue.

   OJO — es un UNIQUE CONSTRAINT, no un indice suelto (sys.indexes.
   is_unique_constraint = 1). Por eso va con ALTER TABLE DROP/ADD CONSTRAINT y no
   con DROP INDEX, que falla sobre un constraint. Ninguna FK depende de el: las
   dos FKs de la tabla (PlatNotaSeccion, PlatNotaReferencia) apuntan a la PK.

   Idempotente: se puede re-correr sin efecto.
   Tabla real: dbo.PlatNotaClinica (PlatNotasClinicas es el DbSet de EF).
   ============================================================================= */

IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE name = 'UQ_PlatNotaClinica_Destino'
             AND parent_object_id = OBJECT_ID('dbo.PlatNotaClinica'))
BEGIN
    ALTER TABLE dbo.PlatNotaClinica DROP CONSTRAINT UQ_PlatNotaClinica_Destino;
    PRINT 'Constraint viejo (TipoDestino, DestinoId) eliminado.';
END
ELSE
    PRINT 'No habia constraint con ese nombre — sin cambios en el DROP.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UQ_PlatNotaClinica_Destino'
                 AND object_id = OBJECT_ID('dbo.PlatNotaClinica'))
BEGIN
    ALTER TABLE dbo.PlatNotaClinica
        ADD CONSTRAINT UQ_PlatNotaClinica_Destino
        UNIQUE (TipoDestino, DestinoId, TipoNota);
    PRINT 'Constraint recreado como (TipoDestino, DestinoId, TipoNota).';
END
ELSE
    PRINT 'Ya existia — sin cambios.';
GO

/* Post-check: deben salir las TRES columnas en orden 1,2,3 */
SELECT c.name AS Col, ic.key_ordinal, i.is_unique, i.is_unique_constraint
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.PlatNotaClinica')
  AND i.name = 'UQ_PlatNotaClinica_Destino'
ORDER BY ic.key_ordinal;
GO
