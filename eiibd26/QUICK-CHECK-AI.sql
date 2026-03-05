-- ===== QUICK CHECK: VERIFICACIÓN RÁPIDA DEL SISTEMA AI =====
-- Ejecuta este script para verificar que todo está listo antes de reiniciar

-- 1. ¿Existe el usuario sistema?
IF EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'nina@eiibd.com')
    SELECT '✅ Usuario sistema EXISTE' AS Resultado
ELSE
    SELECT '❌ Usuario sistema NO EXISTE - Ejecuta SETUP-SYSTEM-USER.sql' AS Resultado;

-- 2. ¿Existe el perfil del usuario sistema?
IF EXISTS (
    SELECT 1 FROM Perfil p
    INNER JOIN AspNetUsers u ON p.idUser = u.Id
    WHERE u.Email = 'nina@eiibd.com'
)
    SELECT '✅ Perfil del usuario sistema EXISTE' AS Resultado
ELSE
    SELECT '❌ Perfil del usuario sistema NO EXISTE - Ejecuta SETUP-SYSTEM-USER.sql' AS Resultado;

-- 3. ¿Existen los campos de migración?
IF COL_LENGTH('Preguntas', 'TieneRespuestaIA') IS NOT NULL
    AND COL_LENGTH('Preguntas', 'FechaGeneracionIA') IS NOT NULL
    AND COL_LENGTH('Respuestas', 'EsIA') IS NOT NULL
    AND COL_LENGTH('Respuestas', 'ModeloIA') IS NOT NULL
    SELECT '✅ Campos de migración EXISTEN' AS Resultado
ELSE
    SELECT '❌ Campos de migración NO EXISTEN - Ejecuta MIGRATION-AI-FIELDS.sql' AS Resultado;

-- 4. Mostrar GUID del usuario sistema para verificar en appsettings.json
SELECT 
    'ℹ️ Copia este GUID y verifica que coincide con AiAnswer:SystemUserId en appsettings.json' AS Instruccion,
    Id AS SystemUserId
FROM AspNetUsers 
WHERE Email = 'nina@eiibd.com';

-- 5. ¿Hay preguntas recientes que deberían tener respuesta IA?
SELECT TOP 3
    p.Id AS PreguntaId,
    LEFT(p.Titulo, 50) AS Titulo,
    DATEDIFF(MINUTE, p.FechaCreacion, GETUTCDATE()) AS MinutosDesdeCreacion,
    p.TieneRespuestaIA,
    (SELECT COUNT(*) FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0) AS TotalRespuestas,
    CASE 
        WHEN p.TieneRespuestaIA = 1 THEN '✅ Ya tiene respuesta IA'
        WHEN (SELECT COUNT(*) FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0 AND r.EsIA = 0) > 0 
            THEN '⚠️ Tiene respuesta humana (IA no debe generarse)'
        ELSE '🔄 Debería generar respuesta IA después de reiniciar'
    END AS Estado
FROM Preguntas p
WHERE p.Eliminado = 0
ORDER BY p.FechaCreacion DESC;

-- 6. Resumen final
PRINT '';
PRINT '==============================================';
PRINT 'RESUMEN:';
PRINT '==============================================';
PRINT '';
PRINT 'Si todos los checks anteriores muestran ✅:';
PRINT '1. Verifica que appsettings.json tenga:';
PRINT '   - AiAnswer.Enabled = true';
PRINT '   - AiAnswer.AnthropicApiKey con tu API key real';
PRINT '   - AiAnswer.SystemUserId con el GUID mostrado arriba';
PRINT '';
PRINT '2. REINICIA la aplicación (importante!)';
PRINT '';
PRINT '3. Crea una nueva pregunta de prueba';
PRINT '';
PRINT '4. Espera 10-30 segundos y recarga la página';
PRINT '';
PRINT '==============================================';
