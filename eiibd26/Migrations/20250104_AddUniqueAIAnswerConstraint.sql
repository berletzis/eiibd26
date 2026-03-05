-- =====================================================
-- PRODUCTION HARDENING: Prevent Duplicate AI Answers
-- Date: 2025-01-04
-- Purpose: Ensure only ONE AI answer per question
-- =====================================================

USE [eiibd26]; -- Change to your database name
GO

-- =====================================================
-- 1. CHECK FOR EXISTING DUPLICATES
-- =====================================================

PRINT '🔍 Checking for existing duplicate AI answers...';

SELECT 
    PreguntaId,
    COUNT(*) AS DuplicateCount
FROM Respuestas
WHERE EsIA = 1 AND Eliminado = 0
GROUP BY PreguntaId
HAVING COUNT(*) > 1;

-- If duplicates found, manual review required:
-- Option A: Keep oldest, mark others as Eliminado
-- Option B: Keep highest scored, mark others as Eliminado

/*
-- Uncomment and run if duplicates exist:
WITH DuplicateAIAnswers AS (
    SELECT 
        Id,
        PreguntaId,
        FechaCreacion,
        Puntuacion,
        ROW_NUMBER() OVER (
            PARTITION BY PreguntaId 
            ORDER BY FechaCreacion ASC, Puntuacion DESC
        ) AS RowNum
    FROM Respuestas
    WHERE EsIA = 1 AND Eliminado = 0
)
UPDATE Respuestas
SET Eliminado = 1,
    FechaModificacion = GETUTCDATE()
WHERE Id IN (
    SELECT Id FROM DuplicateAIAnswers WHERE RowNum > 1
);

PRINT '✅ Duplicate AI answers marked as deleted';
*/

GO

-- =====================================================
-- 2. CREATE UNIQUE FILTERED INDEX
-- =====================================================

PRINT '🔨 Creating unique filtered index...';

-- Drop if exists (for rerunning script)
IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'UX_Respuestas_OneAIAnswerPerQuestion' 
    AND object_id = OBJECT_ID('Respuestas')
)
BEGIN
    DROP INDEX [UX_Respuestas_OneAIAnswerPerQuestion] ON [Respuestas];
    PRINT '⚠️ Existing index dropped';
END

-- Create unique filtered index
CREATE UNIQUE NONCLUSTERED INDEX [UX_Respuestas_OneAIAnswerPerQuestion]
ON [Respuestas]([PreguntaId])
WHERE [EsIA] = 1 AND [Eliminado] = 0;

PRINT '✅ Unique constraint created: Only one AI answer per question allowed';

GO

-- =====================================================
-- 3. VERIFY CONSTRAINT
-- =====================================================

PRINT '';
PRINT '========================================';
PRINT 'VERIFICATION';
PRINT '========================================';

-- Show index details
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.has_filter AS HasFilter,
    i.filter_definition AS FilterDefinition
FROM sys.indexes i
WHERE i.name = 'UX_Respuestas_OneAIAnswerPerQuestion'
AND i.object_id = OBJECT_ID('Respuestas');

-- Test insert (should fail on second insert)
/*
PRINT '';
PRINT '🧪 Testing constraint (should fail on duplicate)...';

DECLARE @TestPreguntaId UNIQUEIDENTIFIER = NEWID();
DECLARE @TestUsuarioId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers);

BEGIN TRY
    -- First insert (should succeed)
    INSERT INTO Respuestas (Id, PreguntaId, UsuarioId, Cuerpo, EsIA, Eliminado, FechaCreacion)
    VALUES (NEWID(), @TestPreguntaId, @TestUsuarioId, 'Test AI Answer 1', 1, 0, GETUTCDATE());
    
    PRINT '✅ First AI answer inserted successfully';
    
    -- Second insert (should fail)
    INSERT INTO Respuestas (Id, PreguntaId, UsuarioId, Cuerpo, EsIA, Eliminado, FechaCreacion)
    VALUES (NEWID(), @TestPreguntaId, @TestUsuarioId, 'Test AI Answer 2', 1, 0, GETUTCDATE());
    
    PRINT '❌ ERROR: Second AI answer should have been blocked!';
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 2601 -- Unique constraint violation
        PRINT '✅ Constraint working: Duplicate AI answer blocked';
    ELSE
        PRINT '⚠️ Unexpected error: ' + ERROR_MESSAGE();
        
    -- Cleanup test data
    DELETE FROM Respuestas WHERE PreguntaId = @TestPreguntaId;
END CATCH
*/

PRINT '';
PRINT '========================================';
PRINT '✅ MIGRATION COMPLETE';
PRINT '========================================';
PRINT '';
PRINT 'Risk Mitigation:';
PRINT '  - Duplicate AI answers: PREVENTED';
PRINT '  - Race conditions: BLOCKED at database level';
PRINT '  - Cost waste: ELIMINATED';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Update AiAnswerJob to handle constraint violations';
PRINT '  2. Test with concurrent job execution';
PRINT '  3. Monitor for constraint violation logs';

GO
