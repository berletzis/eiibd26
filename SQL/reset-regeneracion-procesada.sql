/*
    reset-regeneracion-procesada.sql
    ---------------------------------------------------------------------------
    Limpia la marca de "ya regenerado" para forzar una pasada COMPLETA.

    Cuándo: se mejoró el prompt del generador y se quiere rehacer todo el catálogo.
    NO hace falta para el uso normal — el batch ya reanuda solo por la marca.

    ⚠️ Esto NO borra descripciones: solo vuelve a poner los registros en la cola del
    re-proceso. La próxima corrida les pagará una llamada a la IA a cada uno, así que
    en tratamientos (miles) es dinero real. Correr a conciencia.

    Requiere SQL/add-regeneracion-procesada.sql ya aplicado.

    Por defecto está en modo SOLO LECTURA: muestra a cuántos afectaría.
    Para ejecutar de verdad, cambiar @Ejecutar a 1.
*/

SET NOCOUNT ON;

DECLARE @Ejecutar BIT = 0;   -- ⬅️ 1 = aplica el reset · 0 = solo cuenta

-- Alcance opcional: NULL = todo el catálogo. Con un id de raíz, solo esa rama
-- (la raíz + sus hijos directos; el árbol es de 2 niveles, no hay nietos).
DECLARE @RaizSintomas     INT = NULL;
DECLARE @RaizTratamientos INT = NULL;

SELECT
    tabla      = 'sintomas',
    a_resetear = COUNT(*)
FROM dbo.sintomas
WHERE Eliminado = 0
  AND RegeneracionProcesadaUtc IS NOT NULL
  AND (@RaizSintomas IS NULL OR id = @RaizSintomas OR idPadre = @RaizSintomas)
UNION ALL
SELECT
    'tratamientos',
    COUNT(*)
FROM dbo.tratamientos
WHERE Eliminado = 0
  AND RegeneracionProcesadaUtc IS NOT NULL
  AND (@RaizTratamientos IS NULL OR id = @RaizTratamientos OR idPadre = @RaizTratamientos);

IF @Ejecutar = 1
BEGIN
    BEGIN TRANSACTION;

    UPDATE dbo.sintomas
       SET RegeneracionProcesadaUtc = NULL
     WHERE Eliminado = 0
       AND RegeneracionProcesadaUtc IS NOT NULL
       AND (@RaizSintomas IS NULL OR id = @RaizSintomas OR idPadre = @RaizSintomas);
    PRINT CONCAT('sintomas reseteados: ', @@ROWCOUNT);

    UPDATE dbo.tratamientos
       SET RegeneracionProcesadaUtc = NULL
     WHERE Eliminado = 0
       AND RegeneracionProcesadaUtc IS NOT NULL
       AND (@RaizTratamientos IS NULL OR id = @RaizTratamientos OR idPadre = @RaizTratamientos);
    PRINT CONCAT('tratamientos reseteados: ', @@ROWCOUNT);

    COMMIT TRANSACTION;
END
ELSE
    PRINT 'Modo solo lectura (@Ejecutar = 0). No se cambió nada.';
