-- =====================================================
-- EMERGENCY FIX: Actualizar todos los NULL en EsPrincipal
-- =====================================================
-- Ejecutar esto AHORA en SQL Server
-- =====================================================

BEGIN TRANSACTION;

-- 1. Actualizar TODOS los NULL (sin filtros)
PRINT 'Actualizando TODOS los NULL en EsPrincipal...';
UPDATE dbo.contenidosCategoriasRelacion
SET EsPrincipal = 0,
    FechaModificacion = GETUTCDATE()
WHERE EsPrincipal IS NULL;
PRINT 'Filas actualizadas: ' + CAST(@@ROWCOUNT AS VARCHAR);

-- 2. Verificar que no queden NULL
DECLARE @NullCount INT;
SELECT @NullCount = COUNT(*)
FROM dbo.contenidosCategoriasRelacion
WHERE EsPrincipal IS NULL;

IF @NullCount > 0
BEGIN
    PRINT '⚠️ ADVERTENCIA: Aún quedan ' + CAST(@NullCount AS VARCHAR) + ' valores NULL';
    ROLLBACK TRANSACTION;
END
ELSE
BEGIN
    PRINT '✅ No hay valores NULL';
    
    -- 3. Asegurar que la columna sea NOT NULL
    PRINT 'Verificando constraint NOT NULL...';
    IF EXISTS (
        SELECT 1 
        FROM sys.columns 
        WHERE object_id = OBJECT_ID('dbo.contenidosCategoriasRelacion') 
          AND name = 'EsPrincipal' 
          AND is_nullable = 1
    )
    BEGIN
        PRINT 'Aplicando NOT NULL constraint...';
        ALTER TABLE dbo.contenidosCategoriasRelacion
        ALTER COLUMN EsPrincipal BIT NOT NULL;
        PRINT '✅ Columna ahora es NOT NULL';
    END
    ELSE
    BEGIN
        PRINT '✅ Columna ya es NOT NULL';
    END
    
    -- 4. Agregar DEFAULT constraint si no existe
    IF NOT EXISTS (
        SELECT 1 
        FROM sys.default_constraints 
        WHERE parent_object_id = OBJECT_ID('dbo.contenidosCategoriasRelacion') 
          AND name = 'DF_ContenidosCategoriasRelacion_EsPrincipal'
    )
    BEGIN
        PRINT 'Agregando DEFAULT constraint...';
        ALTER TABLE dbo.contenidosCategoriasRelacion
        ADD CONSTRAINT DF_ContenidosCategoriasRelacion_EsPrincipal DEFAULT (0) FOR EsPrincipal;
        PRINT '✅ DEFAULT constraint agregado';
    END
    ELSE
    BEGIN
        PRINT '✅ DEFAULT constraint ya existe';
    END
    
    COMMIT TRANSACTION;
    PRINT '';
    PRINT '========================================';
    PRINT '✅ FIX COMPLETADO EXITOSAMENTE';
    PRINT '========================================';
END

-- Reporte final
SELECT 
    'Estado Final' AS Reporte,
    COUNT(*) AS TotalRegistros,
    SUM(CASE WHEN EsPrincipal IS NULL THEN 1 ELSE 0 END) AS ConNull,
    SUM(CASE WHEN EsPrincipal = 1 THEN 1 ELSE 0 END) AS ConTrue,
    SUM(CASE WHEN EsPrincipal = 0 THEN 1 ELSE 0 END) AS ConFalse
FROM dbo.contenidosCategoriasRelacion;
