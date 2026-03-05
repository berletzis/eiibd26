-- ===== DIAGNÓSTICO RÁPIDO: ¿Por qué no se genera la respuesta IA? =====

PRINT '========== 1. USUARIO SISTEMA ==========';
-- Verifica si existe el usuario con el GUID configurado en appsettings.json
DECLARE @ConfiguredSystemUserId UNIQUEIDENTIFIER = '50649075-660F-4431-9049-98C9E3AC6D73';

SELECT 
    CASE 
        WHEN EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = @ConfiguredSystemUserId)
        THEN '✅ Usuario sistema EXISTE'
        ELSE '❌ Usuario sistema NO EXISTE - Verifica el GUID en appsettings.json'
    END AS Estado,
    @ConfiguredSystemUserId AS ConfiguredSystemUserId,
    (SELECT UserName FROM AspNetUsers WHERE Id = @ConfiguredSystemUserId) AS UserName,
    (SELECT Email FROM AspNetUsers WHERE Id = @ConfiguredSystemUserId) AS Email;

PRINT '';
PRINT '========== 2. ÚLTIMA PREGUNTA CREADA ==========';
SELECT TOP 1
    p.Id,
    p.Titulo,
    p.Slug,
    p.FechaCreacion,
    DATEDIFF(SECOND, p.FechaCreacion, GETUTCDATE()) AS SegundosDesdeCreacion,
    p.TieneRespuestaIA,
    p.FechaGeneracionIA,
    (SELECT COUNT(*) FROM Respuestas WHERE PreguntaId = p.Id AND Eliminado = 0) AS TotalRespuestas,
    CASE 
        WHEN p.TieneRespuestaIA = 1 THEN '✅ Tiene respuesta IA'
        WHEN (SELECT COUNT(*) FROM Respuestas WHERE PreguntaId = p.Id AND Eliminado = 0 AND EsIA = 0) > 0 
            THEN '⚠️ Tiene respuesta HUMANA (IA no debe generarse)'
        WHEN DATEDIFF(SECOND, p.FechaCreacion, GETUTCDATE()) < 60
            THEN '⏳ ESPERAR (creada hace menos de 60 segundos)'
        ELSE '❌ NO TIENE respuesta IA cuando debería tenerla'
    END AS Diagnostico
FROM Preguntas p
WHERE p.Eliminado = 0
ORDER BY p.FechaCreacion DESC;

PRINT '';
PRINT '========== 3. ¿HAY RESPUESTAS IA EN LA BD? ==========';
SELECT 
    CASE 
        WHEN EXISTS (SELECT 1 FROM Respuestas WHERE EsIA = 1 AND Eliminado = 0)
        THEN CONCAT('✅ SÍ - Total: ', CAST((SELECT COUNT(*) FROM Respuestas WHERE EsIA = 1 AND Eliminado = 0) AS VARCHAR))
        ELSE '❌ NO hay ninguna respuesta IA en la base de datos'
    END AS Estado;

PRINT '';
PRINT '========== 4. CAMPOS DE MIGRACIÓN ==========';
SELECT 
    CASE 
        WHEN COL_LENGTH('Respuestas', 'EsIA') IS NOT NULL 
        THEN '✅ Campo Respuesta.EsIA existe'
        ELSE '❌ Campo Respuesta.EsIA NO EXISTE - EJECUTAR MIGRATION-AI-FIELDS.sql'
    END AS Estado;

PRINT '';
PRINT '========== 5. PREGUNTAS CANDIDATAS PARA IA ==========';
-- Preguntas que NO tienen respuesta IA y NO tienen respuestas humanas
SELECT TOP 5
    p.Id,
    LEFT(p.Titulo, 60) AS Titulo,
    p.FechaCreacion,
    DATEDIFF(MINUTE, p.FechaCreacion, GETUTCDATE()) AS MinutosDesdeCreacion,
    '❌ Debería tener respuesta IA' AS Estado
FROM Preguntas p
WHERE p.Eliminado = 0
AND p.TieneRespuestaIA = 0
AND NOT EXISTS (SELECT 1 FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0)
ORDER BY p.FechaCreacion DESC;

PRINT '';
PRINT '========== RESUMEN ==========';
PRINT 'Si ves ❌ en algún punto, ese es el problema.';
PRINT 'Si todo está ✅ pero no hay respuestas IA, el problema está en la aplicación (logs).';
