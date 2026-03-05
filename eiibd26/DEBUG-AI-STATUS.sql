-- ===== SCRIPT DE DIAGN�STICO: ESTADO DEL SISTEMA AI =====
-- Este script ayuda a diagnosticar por qu� no se generan respuestas de IA

PRINT '========================================';
PRINT '1. VERIFICAR USUARIO SISTEMA';
PRINT '========================================';

SELECT 
    'Usuario Sistema' AS Tipo,
    Id AS UserId,
    UserName,
    Email,
    EmailConfirmed,
    LockoutEnabled,
    LockoutEnd,
    CASE 
        WHEN LockoutEnd IS NOT NULL AND LockoutEnd > GETUTCDATE() THEN 'BLOQUEADO'
        WHEN PasswordHash = 'SYSTEM_NO_LOGIN' THEN 'OK (No puede login)'
        ELSE 'REVISAR'
    END AS Estado
FROM AspNetUsers 
WHERE Email = 'sistema-ia@eiibd.com';

-- Verificar si existe perfil
SELECT 
    'Perfil Sistema' AS Tipo,
    p.idUser,
    p.Nombre,
    p.Avatar,
    p.FechaCreacion,
    CASE 
        WHEN p.idUser IS NULL THEN 'NO EXISTE'
        ELSE 'OK'
    END AS Estado
FROM AspNetUsers u
LEFT JOIN Perfil p ON u.Id = p.idUser
WHERE u.Email = 'sistema-ia@eiibd.com';

PRINT '';
PRINT '========================================';
PRINT '2. VERIFICAR PREGUNTAS SIN RESPUESTA IA';
PRINT '========================================';

-- Mostrar preguntas que deber�an tener respuesta de IA pero no la tienen
SELECT TOP 10
    p.Id AS PreguntaId,
    p.Titulo,
    p.FechaCreacion,
    p.TieneRespuestaIA,
    p.FechaGeneracionIA,
    (SELECT COUNT(*) FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0) AS TotalRespuestas,
    (SELECT COUNT(*) FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0 AND r.EsIA = 1) AS RespuestasIA,
    (SELECT COUNT(*) FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0 AND r.EsIA = 0) AS RespuestasHumanas,
    DATEDIFF(MINUTE, p.FechaCreacion, GETUTCDATE()) AS MinutosDesdeCreacion,
    CASE 
        WHEN p.TieneRespuestaIA = 1 THEN 'Ya tiene respuesta IA'
        WHEN (SELECT COUNT(*) FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0 AND r.EsIA = 0) > 0 THEN 'Tiene respuesta humana (IA no aplica)'
        ELSE 'DEBER�A TENER RESPUESTA IA'
    END AS Estado
FROM Preguntas p
WHERE p.Eliminado = 0
ORDER BY p.FechaCreacion DESC;

PRINT '';
PRINT '========================================';
PRINT '3. VERIFICAR RESPUESTAS DE IA EXISTENTES';
PRINT '========================================';

SELECT TOP 10
    r.Id AS RespuestaId,
    r.PreguntaId,
    p.Titulo AS TituloPregunta,
    r.EsIA,
    r.ModeloIA,
    r.UsuarioId,
    u.Email AS EmailAutor,
    r.FechaCreacion,
    LEN(r.Cuerpo) AS LargoCuerpo,
    r.EsAceptada,
    r.Eliminado
FROM Respuestas r
INNER JOIN Preguntas p ON r.PreguntaId = p.Id
INNER JOIN AspNetUsers u ON r.UsuarioId = u.Id
WHERE r.EsIA = 1
ORDER BY r.FechaCreacion DESC;

PRINT '';
PRINT '========================================';
PRINT '4. ESTAD�STICAS GENERALES';
PRINT '========================================';

SELECT 
    'Total Preguntas' AS Metrica,
    COUNT(*) AS Valor
FROM Preguntas 
WHERE Eliminado = 0
UNION ALL
SELECT 
    'Preguntas con Respuesta IA',
    COUNT(*)
FROM Preguntas 
WHERE Eliminado = 0 AND TieneRespuestaIA = 1
UNION ALL
SELECT 
    'Preguntas sin ninguna respuesta',
    COUNT(*)
FROM Preguntas p
WHERE p.Eliminado = 0 
AND NOT EXISTS (SELECT 1 FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0)
UNION ALL
SELECT 
    'Preguntas solo con respuestas humanas',
    COUNT(*)
FROM Preguntas p
WHERE p.Eliminado = 0 
AND EXISTS (SELECT 1 FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0 AND r.EsIA = 0)
AND NOT EXISTS (SELECT 1 FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0 AND r.EsIA = 1)
UNION ALL
SELECT 
    'Total Respuestas IA generadas',
    COUNT(*)
FROM Respuestas 
WHERE Eliminado = 0 AND EsIA = 1;

PRINT '';
PRINT '========================================';
PRINT '5. VERIFICAR CAMPOS MIGRACI�N';
PRINT '========================================';

-- Verificar que los campos de IA existan en las tablas
SELECT 
    'Pregunta.TieneRespuestaIA' AS Campo,
    CASE WHEN COL_LENGTH('Preguntas', 'TieneRespuestaIA') IS NOT NULL THEN 'EXISTE' ELSE 'NO EXISTE' END AS Estado
UNION ALL
SELECT 
    'Pregunta.FechaGeneracionIA',
    CASE WHEN COL_LENGTH('Preguntas', 'FechaGeneracionIA') IS NOT NULL THEN 'EXISTE' ELSE 'NO EXISTE' END
UNION ALL
SELECT 
    'Respuesta.EsIA',
    CASE WHEN COL_LENGTH('Respuestas', 'EsIA') IS NOT NULL THEN 'EXISTE' ELSE 'NO EXISTE' END
UNION ALL
SELECT 
    'Respuesta.ModeloIA',
    CASE WHEN COL_LENGTH('Respuestas', 'ModeloIA') IS NOT NULL THEN 'EXISTE' ELSE 'NO EXISTE' END
UNION ALL
SELECT 
    'Respuesta.EsColapsada',
    CASE WHEN COL_LENGTH('Respuestas', 'EsColapsada') IS NOT NULL THEN 'EXISTE' ELSE 'NO EXISTE' END;

PRINT '';
PRINT '========================================';
PRINT '6. �LTIMAS PREGUNTAS CREADAS (PARA PRUEBAS)';
PRINT '========================================';

SELECT TOP 5
    p.Id AS PreguntaId,
    p.Titulo,
    p.Slug,
    p.FechaCreacion,
    DATEDIFF(MINUTE, p.FechaCreacion, GETUTCDATE()) AS MinutosAtras,
    p.TieneRespuestaIA,
    (SELECT COUNT(*) FROM Respuestas r WHERE r.PreguntaId = p.Id AND r.Eliminado = 0) AS TotalRespuestas,
    '/Preguntas/' + p.Slug AS URL
FROM Preguntas p
WHERE p.Eliminado = 0
ORDER BY p.FechaCreacion DESC;

PRINT '';
PRINT '========================================';
PRINT 'DIAGN�STICO COMPLETADO';
PRINT '========================================';
