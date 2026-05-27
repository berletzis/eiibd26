-- ============================================================
-- FASE 2 — Reset seguro de un profesional en el directorio
-- Proyecto EIIBD · 2026-05-27
-- ============================================================
-- Propósito: limpiar el estado de claim/verificación de un
--   profesional sin borrar datos históricos (confirmaciones,
--   perfil extendido, etc.).
--
-- IMPORTANTE:
--   - Reemplaza @MedicoId con el Id del registro en MedicosDirectorio.
--   - Ejecutar SIEMPRE dentro de una transacción explícita.
--   - Verificar con el SELECT de auditoría ANTES de ejecutar.
--   - NO elimina el registro del profesional (soft delete ≠ reset).
-- ============================================================

-- PASO 0 ── Verificar el estado actual ANTES del reset
-- ─────────────────────────────────────────────────────
SELECT
    m.Id,
    m.NombreCompleto,
    m.EstatusValidacion,   -- 0=Pendiente, 1=Validado, 2=NoVerificado, 3=Rechazado
    m.EstatusReclamacion,  -- 0=NoReclamado, 1=EnProceso, 2=Reclamado, 3=Rechazado
    m.NivelConfianza,      -- 0=Identificado, 1=Confirmado, 2=Reconocido, 3=Establecido
    m.AspNetUserId,
    m.FechaCedulaVerificada,
    m.EmailSolicitudClaim,
    m.FechaSolicitudClaim,
    m.FechaReclamacion,
    m.Activo,
    m.VisiblePublicamente,
    m.Eliminado,
    badges_count = (SELECT COUNT(*) FROM dbo.MedicoPerfilBadge WHERE MedicoId = m.Id),
    confs_count  = (SELECT COUNT(*) FROM dbo.ConfirmacionComunitaria WHERE MedicoDirectorioId = m.Id AND Eliminado = 0)
FROM dbo.MedicosDirectorio m
WHERE m.Id = @MedicoId;


-- PASO 1 ── Ver qué badges tiene actualmente
-- ─────────────────────────────────────────────────────
SELECT
    pb.Id,
    pb.MedicoId,
    b.Codigo,
    b.Nombre,
    pb.FechaObtenido,
    pb.OtorgadoPor
FROM dbo.MedicoPerfilBadge pb
INNER JOIN dbo.MedicoBadge b ON b.Id = pb.BadgeId
WHERE pb.MedicoId = @MedicoId
ORDER BY pb.FechaObtenido;


-- ════════════════════════════════════════════════════════════
-- RESET — Ejecutar dentro de transacción
-- ════════════════════════════════════════════════════════════
BEGIN TRANSACTION;
BEGIN TRY

    -- R-01: Eliminar todos los badges del profesional
    DELETE FROM dbo.MedicoPerfilBadge
    WHERE MedicoId = @MedicoId;

    -- R-02: Recalcular NivelConfianza basado en confirmaciones activas
    --       (sin claim ni cédula verificada → máximo nivel 2 si >=5 confirmaciones)
    DECLARE @TotalConfs INT;
    SELECT @TotalConfs = COUNT(*)
    FROM dbo.ConfirmacionComunitaria
    WHERE MedicoDirectorioId = @MedicoId AND Eliminado = 0;

    DECLARE @NuevoNivel INT;
    SET @NuevoNivel =
        CASE
            WHEN @TotalConfs >= 5 THEN 2  -- Reconocido (cédula no verificada, pero confirmaciones)
            WHEN @TotalConfs >= 3 THEN 1  -- Confirmado
            ELSE 0                         -- Identificado
        END;

    -- R-03: Limpiar campos de claim/verificación y actualizar nivel
    UPDATE dbo.MedicosDirectorio
    SET
        EstatusValidacion    = 0,        -- PendienteValidacion
        EstatusReclamacion   = 0,        -- NoReclamado
        NivelConfianza       = @NuevoNivel,
        AspNetUserId         = NULL,
        FechaCedulaVerificada = NULL,
        EmailSolicitudClaim  = NULL,
        FechaSolicitudClaim  = NULL,
        FechaReclamacion     = NULL,
        FechaModificacion    = SYSUTCDATETIME()
        -- NO se toca: NombreCompleto, Especialidad, Activo, VisiblePublicamente,
        --             Eliminado, CedulaProfesional, ubicación, PropuestoPorUsuarioId
    WHERE Id = @MedicoId;

    -- NOTA: MedicoPerfilExtendido se deja intacto. Si el médico reclamó
    -- su perfil, ese contenido (foto, bio, hospitales) puede ser valioso.
    -- Si se requiere limpiar también el perfil extendido, hacerlo en un
    -- script separado y documentarlo explícitamente.

    COMMIT TRANSACTION;

    -- PASO 2 ── Verificar estado POST-reset
    SELECT
        m.Id,
        m.NombreCompleto,
        m.EstatusValidacion,
        m.EstatusReclamacion,
        m.NivelConfianza,
        m.AspNetUserId,
        m.FechaCedulaVerificada,
        m.EmailSolicitudClaim,
        m.FechaReclamacion,
        badges_restantes = (SELECT COUNT(*) FROM dbo.MedicoPerfilBadge WHERE MedicoId = m.Id),
        confs_intactas   = (SELECT COUNT(*) FROM dbo.ConfirmacionComunitaria WHERE MedicoDirectorioId = m.Id AND Eliminado = 0)
    FROM dbo.MedicosDirectorio m
    WHERE m.Id = @MedicoId;

    PRINT 'Reset completado correctamente para MedicoId = ' + CAST(@MedicoId AS VARCHAR(10));

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH;


-- ════════════════════════════════════════════════════════════
-- RESET EXTENDIDO (opcional) — limpiar también MedicoPerfilExtendido
-- Solo si se requiere desligar completamente al usuario del perfil.
-- Ejecutar DESPUÉS del reset principal y de forma independiente.
-- ════════════════════════════════════════════════════════════
/*
BEGIN TRANSACTION;
BEGIN TRY

    -- Desvincular UserId del perfil extendido (NO eliminar el perfil)
    UPDATE dbo.MedicoPerfilExtendido
    SET
        UserId           = NULL,
        FechaModificacion = SYSUTCDATETIME()
    WHERE MedicoId = @MedicoId;

    COMMIT TRANSACTION;
    PRINT 'UserId desvinculado del perfil extendido para MedicoId = ' + CAST(@MedicoId AS VARCHAR(10));

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR reset extendido: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
*/
