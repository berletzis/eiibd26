-- ================================================================
-- ELIMINACIÓN COMPLETA DE USUARIO
-- Reemplazar 'usuario_a_eliminar' con el UserName real.
-- Revisar cuidadosamente antes de ejecutar.
-- ================================================================

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @UserId UNIQUEIDENTIFIER;
    SELECT @UserId = Id FROM dbo.AspNetUsers WHERE UserName = 'usuario_a_eliminar';

    -- Guard: cancelar si el usuario no existe
    IF @UserId IS NULL
    BEGIN
        RAISERROR('Usuario no encontrado. Operación cancelada.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- ── BLOQUE 1: Datos clínicos personales ──────────────────────────
    DELETE FROM dbo.condicionUsuario            WHERE idUsuario = @UserId;
    DELETE FROM dbo.sintomasUsuario             WHERE idUsuario = @UserId;
    DELETE FROM dbo.SintomasNotas               WHERE UsuarioId = @UserId;
    DELETE FROM dbo.tratamientoUsuario          WHERE idUsuario = @UserId;
    DELETE FROM dbo.TratamientosNotas           WHERE UsuarioId = @UserId;
    DELETE FROM dbo.EstadoAnimoUsuario          WHERE IdUsuario = @UserId;
    DELETE FROM dbo.estudiosLabUsuario          WHERE idUsuario = @UserId;
    DELETE FROM dbo.TrackingSintomaUsuario      WHERE IdUsuario = @UserId;
    DELETE FROM dbo.TratamientoCondicionUsuario WHERE IdUsuario = @UserId;
    DELETE FROM dbo.TratamientoSintomaUsuario   WHERE IdUsuario = @UserId;
    DELETE FROM dbo.SintomaCondicionUsuario     WHERE IdUsuario = @UserId;
    DELETE FROM dbo.evaluacionRespuestasUsuario WHERE idUsuario = @UserId;

    -- ── BLOQUE 2: Ratings y feedback propios ─────────────────────────
    DELETE FROM dbo.Votos                                     WHERE UsuarioId = @UserId;
    DELETE FROM dbo.ArticleRatings                            WHERE UserId    = @UserId;
    DELETE FROM dbo.contenidosCalificacion_ArticulosPreguntas WHERE idUsuario = @UserId;
    DELETE FROM dbo.contenidosCalificacion_Respuestas         WHERE idUsuario = @UserId;
    DELETE FROM dbo.GlossaryTermRatings                       WHERE UserId    = @UserId;
    DELETE FROM dbo.GlossaryValidation                        WHERE UserId    = @UserId;
    DELETE FROM dbo.RespuestaAIFeedback                       WHERE UsuarioId = @UserId;

    -- ── BLOQUE 3: Notificaciones ──────────────────────────────────────
    DELETE FROM dbo.NotificationSubscriptions WHERE UserId    = @UserId;
    DELETE FROM dbo.PushNotifications          WHERE CreatedBy = @UserId;
    DELETE FROM dbo.EmailCampanaLog            WHERE UserId    = @UserId;

    -- ── BLOQUE 4: Respuestas del usuario a preguntas de otros ─────────
    -- Desanclar hijos que apuntan a respuestas de este usuario
    UPDATE dbo.Respuestas SET ParentRespuestaId = NULL
        WHERE ParentRespuestaId IN (SELECT Id FROM dbo.Respuestas WHERE UsuarioId = @UserId);
    DELETE FROM dbo.RespuestaAIFeedback
        WHERE RespuestaId IN (SELECT Id FROM dbo.Respuestas WHERE UsuarioId = @UserId);
    DELETE FROM dbo.contenidosRespuestasRelacion
        WHERE RespuestaId IN (SELECT Id FROM dbo.Respuestas WHERE UsuarioId = @UserId);
    DELETE FROM dbo.Respuestas WHERE UsuarioId = @UserId;

    -- ── BLOQUE 5: Preguntas del usuario y toda su descendencia ─────────
    -- Hijos de respuestas dentro de estas preguntas
    UPDATE dbo.Respuestas SET ParentRespuestaId = NULL
        WHERE ParentRespuestaId IN (
            SELECT Id FROM dbo.Respuestas
            WHERE PreguntaId IN (SELECT Id FROM dbo.Preguntas WHERE UsuarioId = @UserId));
    DELETE FROM dbo.RespuestaAIFeedback
        WHERE RespuestaId IN (
            SELECT Id FROM dbo.Respuestas
            WHERE PreguntaId IN (SELECT Id FROM dbo.Preguntas WHERE UsuarioId = @UserId));
    DELETE FROM dbo.contenidosRespuestasRelacion
        WHERE RespuestaId IN (
            SELECT Id FROM dbo.Respuestas
            WHERE PreguntaId IN (SELECT Id FROM dbo.Preguntas WHERE UsuarioId = @UserId));
    -- Respuestas de otros en las preguntas del usuario
    DELETE FROM dbo.Respuestas
        WHERE PreguntaId IN (SELECT Id FROM dbo.Preguntas WHERE UsuarioId = @UserId);
    -- Relaciones de las preguntas del usuario
    DELETE FROM dbo.PreguntaCondiciones
        WHERE PreguntaId IN (SELECT Id FROM dbo.Preguntas WHERE UsuarioId = @UserId);
    DELETE FROM dbo.PreguntaEtiquetas
        WHERE PreguntaId IN (SELECT Id FROM dbo.Preguntas WHERE UsuarioId = @UserId);
    DELETE FROM dbo.PreguntaSintomas
        WHERE PreguntaId IN (SELECT Id FROM dbo.Preguntas WHERE UsuarioId = @UserId);
    DELETE FROM dbo.PreguntaTratamientos
        WHERE PreguntaId IN (SELECT Id FROM dbo.Preguntas WHERE UsuarioId = @UserId);
    DELETE FROM dbo.contenidosPreguntasRelacion
        WHERE PreguntaId IN (SELECT Id FROM dbo.Preguntas WHERE UsuarioId = @UserId);
    DELETE FROM dbo.Preguntas WHERE UsuarioId = @UserId;

    -- ── BLOQUE 6: Contenidos del usuario y sus relaciones hijo ─────────
    -- Todas las tablas con FK a contenidos.id deben ir antes del DELETE de contenidos
    DELETE FROM dbo.contenidoCondicionesRelacion
        WHERE ContenidoId IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    DELETE FROM dbo.contenidoSintomasRelacion
        WHERE ContenidoId IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    DELETE FROM dbo.contenidoTratamientosRelacion
        WHERE ContenidoId IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    DELETE FROM dbo.contenidosPreguntasRelacion
        WHERE idContenido IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    DELETE FROM dbo.contenidosRespuestasRelacion
        WHERE idContenido IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    DELETE FROM dbo.contenidosCalificacion_ArticulosPreguntas
        WHERE idContenido IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    DELETE FROM dbo.ArticleRatings
        WHERE ArticleId IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    DELETE FROM dbo.ContenidosTag
        WHERE Contenido IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    -- contenidosCategoriasRelacion tiene FK tanto en idContenido como en idPerfil
    DELETE FROM dbo.contenidosCategoriasRelacion
        WHERE idContenido IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId)
           OR idPerfil = @UserId;
    DELETE FROM dbo.contenidosRelacionados
        WHERE IdContenido           IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId)
           OR IdContenidoRelacionado IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    -- Respuestas de otros en los contenidos del usuario (FK IdAutor → Perfil)
    UPDATE dbo.contenidosRespuestas SET IdAutor = NULL
        WHERE idContenidos IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    DELETE FROM dbo.contenidosRespuestas
        WHERE idContenidos IN (SELECT id FROM dbo.contenidos WHERE idUser = @UserId);
    DELETE FROM dbo.contenidos WHERE idUser = @UserId;

    -- ── BLOQUE 7: Referencias a este usuario en contenidos de otros ────
    -- (FK IdAutor → Perfil.idUser; deben limpiarse antes de borrar Perfil)
    UPDATE dbo.contenidos           SET IdAutor = NULL WHERE IdAutor = @UserId;
    UPDATE dbo.contenidosRespuestas SET IdAutor = NULL WHERE IdAutor = @UserId;

    -- ── BLOQUE 8: NULL en columnas de auditoría de tablas compartidas ──
    -- No se borran filas — son datos de otros usuarios/contenidos
    UPDATE dbo.ContenidosTag                SET UsuarioCreacion   = NULL WHERE UsuarioCreacion   = @UserId;
    UPDATE dbo.ContenidosTag                SET UsuarioModificacion = NULL WHERE UsuarioModificacion = @UserId;
    UPDATE dbo.contenidosCategorias         SET UsuarioCreacion   = NULL WHERE UsuarioCreacion   = @UserId;
    UPDATE dbo.contenidosCategorias         SET UsuarioModificacion = NULL WHERE UsuarioModificacion = @UserId;
    UPDATE dbo.contenidosRelacionados       SET UsuarioCreacion   = NULL WHERE UsuarioCreacion   = @UserId;
    UPDATE dbo.contenidosRelacionados       SET UsuarioModificacion = NULL WHERE UsuarioModificacion = @UserId;
    UPDATE dbo.contenidoCondicionesRelacion SET UsuarioCreacion   = NULL WHERE UsuarioCreacion   = @UserId;
    UPDATE dbo.contenidoCondicionesRelacion SET UsuarioModificacion = NULL WHERE UsuarioModificacion = @UserId;
    UPDATE dbo.contenidoSintomasRelacion    SET UsuarioCreacion   = NULL WHERE UsuarioCreacion   = @UserId;
    UPDATE dbo.contenidoSintomasRelacion    SET UsuarioModificacion = NULL WHERE UsuarioModificacion = @UserId;
    UPDATE dbo.contenidosPreguntasRelacion  SET UsuarioCreacion   = NULL WHERE UsuarioCreacion   = @UserId;
    UPDATE dbo.contenidosPreguntasRelacion  SET UsuarioModificacion = NULL WHERE UsuarioModificacion = @UserId;
    UPDATE dbo.contenidosRespuestasRelacion SET UsuarioCreacion   = NULL WHERE UsuarioCreacion   = @UserId;
    UPDATE dbo.contenidosRespuestasRelacion SET UsuarioModificacion = NULL WHERE UsuarioModificacion = @UserId;
    UPDATE dbo.contenidoTratamientosRelacion SET UsuarioCreacion  = NULL WHERE UsuarioCreacion   = @UserId;
    UPDATE dbo.contenidoTratamientosRelacion SET UsuarioModificacion = NULL WHERE UsuarioModificacion = @UserId;
    UPDATE dbo.contenidosCategoriasRelacion SET UsuarioCreacion   = NULL WHERE UsuarioCreacion   = @UserId;
    UPDATE dbo.contenidosCategoriasRelacion SET UsuarioModificacion = NULL WHERE UsuarioModificacion = @UserId;

    -- ── BLOQUE 9: Perfil (después de limpiar FKs que apuntan a él) ────
    DELETE FROM dbo.Aplicaciones  WHERE idPerfil = @UserId;
    DELETE FROM dbo.BadgesDePerfil WHERE idUser  = @UserId;
    DELETE FROM dbo.Perfil         WHERE idUser  = @UserId;

    -- ── BLOQUE 10: Identity (obligatorio antes de AspNetUsers) ─────────
    DELETE FROM dbo.AspNetUserTokens WHERE UserId = @UserId;
    DELETE FROM dbo.AspNetUserLogins WHERE UserId = @UserId;
    DELETE FROM dbo.AspNetUserClaims WHERE UserId = @UserId;
    DELETE FROM dbo.AspNetUserRoles  WHERE UserId = @UserId;

    -- ── BLOQUE 11: Usuario ─────────────────────────────────────────────
    DELETE FROM dbo.AspNetUsers WHERE Id = @UserId;

    COMMIT TRANSACTION;
    PRINT 'Usuario eliminado correctamente: ' + CAST(@UserId AS NVARCHAR(50));

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT            = ERROR_LINE();
    RAISERROR('Error en línea %d: %s', 16, 1, @ErrLine, @ErrMsg);
END CATCH;
