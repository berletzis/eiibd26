-- =====================================================
-- VERIFICACIÓN COMPLETA DEL SISTEMA DE IA
-- Ejecuta este script para verificar que todo está listo
-- =====================================================

USE [eiibd26];
GO

PRINT '========================================';
PRINT '🔍 VERIFICACIÓN DEL SISTEMA DE IA';
PRINT '========================================';
PRINT '';

-- =====================================================
-- 1. VERIFICAR USUARIO DEL SISTEMA
-- =====================================================

PRINT '1️⃣ Verificando usuario del sistema...';
PRINT '';

DECLARE @SystemUserId UNIQUEIDENTIFIER = '50649075-660F-4431-9049-98C9E3AC6D73';

IF EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @SystemUserId)
BEGIN
    SELECT 
        '✅ USUARIO EXISTE' AS Estado,
        Id,
        UserName,
        Email,
        EmailConfirmed
    FROM AspNetUsers 
    WHERE Id = @SystemUserId;
    
    PRINT '✅ Usuario del sistema encontrado correctamente';
END
ELSE
BEGIN
    PRINT '❌ ERROR: Usuario del sistema NO existe';
    PRINT 'Ejecuta SETUP-SYSTEM-USER.sql para crearlo';
END

PRINT '';

-- =====================================================
-- 2. VERIFICAR CAMPOS AI EN TABLA RESPUESTAS
-- =====================================================

PRINT '2️⃣ Verificando campos AI en tabla Respuestas...';
PRINT '';

SELECT 
    'Respuestas' AS Tabla,
    CASE WHEN COL_LENGTH('Respuestas', 'EsIA') IS NOT NULL THEN '✅ EXISTE' ELSE '❌ FALTA' END AS EsIA,
    CASE WHEN COL_LENGTH('Respuestas', 'ModeloIA') IS NOT NULL THEN '✅ EXISTE' ELSE '❌ FALTA' END AS ModeloIA,
    CASE WHEN COL_LENGTH('Respuestas', 'EsColapsada') IS NOT NULL THEN '✅ EXISTE' ELSE '❌ FALTA' END AS EsColapsada,
    CASE WHEN COL_LENGTH('Respuestas', 'Puntuacion') IS NOT NULL THEN '✅ EXISTE' ELSE '❌ FALTA' END AS Puntuacion;

IF COL_LENGTH('Respuestas', 'EsIA') IS NOT NULL
    PRINT '✅ Campos AI en Respuestas OK';
ELSE
BEGIN
    PRINT '❌ ERROR: Faltan campos AI en Respuestas';
    PRINT 'Ejecuta MIGRATION-AI-FIELDS.sql para agregarlos';
END

PRINT '';

-- =====================================================
-- 3. VERIFICAR CAMPOS AI EN TABLA PREGUNTAS
-- =====================================================

PRINT '3️⃣ Verificando campos AI en tabla Preguntas...';
PRINT '';

SELECT 
    'Preguntas' AS Tabla,
    CASE WHEN COL_LENGTH('Preguntas', 'TieneRespuestaIA') IS NOT NULL THEN '✅ EXISTE' ELSE '❌ FALTA' END AS TieneRespuestaIA,
    CASE WHEN COL_LENGTH('Preguntas', 'FechaGeneracionIA') IS NOT NULL THEN '✅ EXISTE' ELSE '❌ FALTA' END AS FechaGeneracionIA;

IF COL_LENGTH('Preguntas', 'TieneRespuestaIA') IS NOT NULL
    PRINT '✅ Campos AI en Preguntas OK';
ELSE
BEGIN
    PRINT '❌ ERROR: Faltan campos AI en Preguntas';
    PRINT 'Ejecuta MIGRATION-AI-FIELDS.sql para agregarlos';
END

PRINT '';

-- =====================================================
-- 4. VERIFICAR CONSTRAINT ÚNICO
-- =====================================================

PRINT '4️⃣ Verificando constraint único para prevenir duplicados...';
PRINT '';

IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'UX_Respuestas_OneAIAnswerPerQuestion' 
    AND object_id = OBJECT_ID('Respuestas')
)
BEGIN
    SELECT 
        '✅ CONSTRAINT EXISTE' AS Estado,
        i.name AS IndexName,
        i.type_desc AS IndexType,
        i.is_unique AS IsUnique,
        i.filter_definition AS FilterDefinition
    FROM sys.indexes i
    WHERE i.name = 'UX_Respuestas_OneAIAnswerPerQuestion'
    AND i.object_id = OBJECT_ID('Respuestas');
    
    PRINT '✅ Constraint de duplicados configurado correctamente';
END
ELSE
BEGIN
    PRINT '❌ WARNING: Constraint único NO existe';
    PRINT 'Ejecuta 20250104_AddUniqueAIAnswerConstraint.sql para crearlo';
    PRINT 'Sin esto, pueden generarse respuestas IA duplicadas';
END

PRINT '';

-- =====================================================
-- 5. VERIFICAR RESPUESTAS IA EXISTENTES
-- =====================================================

PRINT '5️⃣ Verificando respuestas IA existentes...';
PRINT '';

DECLARE @CountRespuestasIA INT = (SELECT COUNT(*) FROM Respuestas WHERE EsIA = 1 AND Eliminado = 0);

SELECT 
    COUNT(*) AS TotalRespuestasIA,
    CASE 
        WHEN COUNT(*) = 0 THEN 'ℹ️ Ninguna respuesta IA generada todavía'
        ELSE '✅ Sistema funcionando - hay respuestas IA'
    END AS Estado
FROM Respuestas 
WHERE EsIA = 1 AND Eliminado = 0;

IF @CountRespuestasIA > 0
BEGIN
    PRINT '✅ Hay ' + CAST(@CountRespuestasIA AS VARCHAR) + ' respuesta(s) IA en el sistema';
    PRINT '';
    PRINT 'Últimas respuestas IA generadas:';
    
    SELECT TOP 5
        p.Titulo AS Pregunta,
        LEFT(r.Cuerpo, 50) + '...' AS Preview,
        r.ModeloIA,
        r.FechaCreacion
    FROM Respuestas r
    INNER JOIN Preguntas p ON p.Id = r.PreguntaId
    WHERE r.EsIA = 1 AND r.Eliminado = 0
    ORDER BY r.FechaCreacion DESC;
END
ELSE
    PRINT 'ℹ️ Ninguna respuesta IA generada todavía (normal si acabas de configurar)';

PRINT '';

-- =====================================================
-- 6. RESUMEN FINAL
-- =====================================================

PRINT '========================================';
PRINT '📊 RESUMEN DE VERIFICACIÓN';
PRINT '========================================';
PRINT '';

DECLARE @TodoOK BIT = 1;

-- Check usuario
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @SystemUserId)
BEGIN
    PRINT '❌ Usuario del sistema: FALTA';
    SET @TodoOK = 0;
END
ELSE
    PRINT '✅ Usuario del sistema: OK';

-- Check campos Respuestas
IF COL_LENGTH('Respuestas', 'EsIA') IS NULL
BEGIN
    PRINT '❌ Campos AI en Respuestas: FALTAN';
    SET @TodoOK = 0;
END
ELSE
    PRINT '✅ Campos AI en Respuestas: OK';

-- Check campos Preguntas
IF COL_LENGTH('Preguntas', 'TieneRespuestaIA') IS NULL
BEGIN
    PRINT '❌ Campos AI en Preguntas: FALTAN';
    SET @TodoOK = 0;
END
ELSE
    PRINT '✅ Campos AI en Preguntas: OK';

-- Check constraint
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'UX_Respuestas_OneAIAnswerPerQuestion' 
    AND object_id = OBJECT_ID('Respuestas')
)
BEGIN
    PRINT '⚠️ Constraint único: FALTA (recomendado)';
END
ELSE
    PRINT '✅ Constraint único: OK';

PRINT '';

IF @TodoOK = 1
BEGIN
    PRINT '========================================';
    PRINT '✅ ¡SISTEMA LISTO PARA USAR!';
    PRINT '========================================';
    PRINT '';
    PRINT 'Siguiente paso:';
    PRINT '1. Reinicia tu aplicación (Shift+F5, luego F5)';
    PRINT '2. Crea una pregunta de prueba';
    PRINT '3. Espera 10-15 segundos';
    PRINT '4. Refresca la página';
    PRINT '5. Verifica el badge: 🤖 Respuesta Informativa (IA)';
END
ELSE
BEGIN
    PRINT '========================================';
    PRINT '⚠️ CONFIGURACIÓN INCOMPLETA';
    PRINT '========================================';
    PRINT '';
    PRINT 'Ejecuta los scripts faltantes y vuelve a verificar.';
END

GO
