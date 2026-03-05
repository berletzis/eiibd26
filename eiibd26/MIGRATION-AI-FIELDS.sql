-- ===== MIGRACION: Agregar campos AI a Pregunta y Respuesta =====
-- Ejecutar estos comandos SQL O crear migración con EF Core

-- 1. Agregar campos a Respuesta
ALTER TABLE [Respuestas] ADD [EsIA] bit NOT NULL DEFAULT 0;
ALTER TABLE [Respuestas] ADD [ModeloIA] nvarchar(100) NULL;
ALTER TABLE [Respuestas] ADD [EsColapsada] bit NOT NULL DEFAULT 0;
ALTER TABLE [Respuestas] ADD [Puntuacion] int NOT NULL DEFAULT 0;

-- 2. Agregar campos a Pregunta
ALTER TABLE [Preguntas] ADD [TieneRespuestaIA] bit NOT NULL DEFAULT 0;
ALTER TABLE [Preguntas] ADD [FechaGeneracionIA] datetimeoffset(7) NULL;

-- 3. Crear índices para mejorar rendimiento de queries
CREATE INDEX IX_Respuestas_PreguntaId_EsIA_Eliminado 
ON [Respuestas]([PreguntaId], [EsIA], [Eliminado])
INCLUDE ([EsAceptada], [Puntuacion], [FechaCreacion]);

CREATE INDEX IX_Preguntas_TieneRespuestaIA_Eliminado 
ON [Preguntas]([TieneRespuestaIA], [Eliminado]);

-- ===== FIN MIGRACION =====


-- ===== O USAR EF CORE MIGRATIONS =====
-- En PowerShell/Terminal:
-- dotnet ef migrations add AddAiAnswerFields
-- dotnet ef database update
