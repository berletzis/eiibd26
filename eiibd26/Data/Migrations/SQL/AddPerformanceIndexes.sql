-- =====================================================================
-- Performance Indexes for User-Scoped Queries
-- Addresses timeout issues on UsuarioSintomas / UsuarioTratamientos pages
-- Run this script against the eiibd26 database
-- =====================================================================

-- sintomasUsuario: filtered index on (idUsuario, Eliminado)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_sintomasUsuario_idUsuario_Eliminado' AND object_id = OBJECT_ID('sintomasUsuario'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_sintomasUsuario_idUsuario_Eliminado]
        ON [sintomasUsuario] ([idUsuario], [Eliminado])
        INCLUDE ([id], [idSintoma], [fechaInicio], [fechaCreado]);
    PRINT 'Created IX_sintomasUsuario_idUsuario_Eliminado';
END

-- condicionUsuario: filtered index on (idUsuario, Eliminado)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_condicionUsuario_idUsuario_Eliminado' AND object_id = OBJECT_ID('condicionUsuario'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_condicionUsuario_idUsuario_Eliminado]
        ON [condicionUsuario] ([idUsuario], [Eliminado])
        INCLUDE ([id], [idCondicion]);
    PRINT 'Created IX_condicionUsuario_idUsuario_Eliminado';
END

-- tratamientoUsuario: filtered index on (idUsuario, Eliminado)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tratamientoUsuario_idUsuario_Eliminado' AND object_id = OBJECT_ID('tratamientoUsuario'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_tratamientoUsuario_idUsuario_Eliminado]
        ON [tratamientoUsuario] ([idUsuario], [Eliminado])
        INCLUDE ([id], [idTratamiento], [fechaInicio], [fechaCreado]);
    PRINT 'Created IX_tratamientoUsuario_idUsuario_Eliminado';
END

-- SintomaCondicionUsuario: index on (IdUsuario, IdSintomaUsuario)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SintomaCondicionUsuario_IdUsuario' AND object_id = OBJECT_ID('SintomaCondicionUsuario'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SintomaCondicionUsuario_IdUsuario]
        ON [SintomaCondicionUsuario] ([IdUsuario], [IdSintomaUsuario])
        INCLUDE ([IdCondicionUsuario]);
    PRINT 'Created IX_SintomaCondicionUsuario_IdUsuario';
END

-- TratamientoSintomaUsuario: index on (IdUsuario, IdSintomaUsuario)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TratamientoSintomaUsuario_IdUsuario' AND object_id = OBJECT_ID('TratamientoSintomaUsuario'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TratamientoSintomaUsuario_IdUsuario]
        ON [TratamientoSintomaUsuario] ([IdUsuario], [IdSintomaUsuario])
        INCLUDE ([IdTratamientoUsuario]);
    PRINT 'Created IX_TratamientoSintomaUsuario_IdUsuario';
END

-- TratamientoSintomaUsuario: index on (IdTratamientoUsuario, IdUsuario)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TratamientoSintomaUsuario_IdTratamientoUsuario' AND object_id = OBJECT_ID('TratamientoSintomaUsuario'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TratamientoSintomaUsuario_IdTratamientoUsuario]
        ON [TratamientoSintomaUsuario] ([IdTratamientoUsuario], [IdUsuario])
        INCLUDE ([IdSintomaUsuario]);
    PRINT 'Created IX_TratamientoSintomaUsuario_IdTratamientoUsuario';
END

-- TratamientoCondicionUsuario: index on (IdTratamientoUsuario, IdUsuario)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TratamientoCondicionUsuario_IdTratamientoUsuario' AND object_id = OBJECT_ID('TratamientoCondicionUsuario'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TratamientoCondicionUsuario_IdTratamientoUsuario]
        ON [TratamientoCondicionUsuario] ([IdTratamientoUsuario], [IdUsuario])
        INCLUDE ([IdCondicionUsuario]);
    PRINT 'Created IX_TratamientoCondicionUsuario_IdTratamientoUsuario';
END

-- TrackingSintomaUsuario: index on (IdUsuario, IdSintomaUsuario)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrackingSintomaUsuario_IdUsuario_IdSintomaUsuario' AND object_id = OBJECT_ID('TrackingSintomaUsuario'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TrackingSintomaUsuario_IdUsuario_IdSintomaUsuario]
        ON [TrackingSintomaUsuario] ([IdUsuario], [IdSintomaUsuario])
        INCLUDE ([Fecha], [Estado]);
    PRINT 'Created IX_TrackingSintomaUsuario_IdUsuario_IdSintomaUsuario';
END

-- EstadoAnimoUsuario: index on (idUsuario) for Dashboard queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EstadoAnimoUsuario_idUsuario' AND object_id = OBJECT_ID('EstadoAnimoUsuario'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EstadoAnimoUsuario_idUsuario]
        ON [EstadoAnimoUsuario] ([idUsuario]);
    PRINT 'Created IX_EstadoAnimoUsuario_idUsuario';
END

-- =====================================================================
-- Home Page Indexes (Contenidos and relation tables)
-- =====================================================================

-- Contenidos: covering index for published content queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Contenidos_Eliminado_EstadoPublicacion_FechaCreado' AND object_id = OBJECT_ID('Contenidos'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Contenidos_Eliminado_EstadoPublicacion_FechaCreado]
        ON [Contenidos] ([Eliminado], [EstadoPublicacion], [FechaCreado] DESC)
        INCLUDE ([Id], [ContenidoTitulo], [ContenidoTituloSlug], [ContenidoTextoC], [URLImagenPrincipal], [Autor], [AutorPerfilId]);
    PRINT 'Created IX_Contenidos_Eliminado_EstadoPublicacion_FechaCreado';
END

-- ContenidoCondiciones: index on (ContenidoId, Borrado)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContenidoCondiciones_ContenidoId_Borrado' AND object_id = OBJECT_ID('ContenidoCondiciones'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ContenidoCondiciones_ContenidoId_Borrado]
        ON [ContenidoCondiciones] ([ContenidoId], [Borrado])
        INCLUDE ([CondicionId]);
    PRINT 'Created IX_ContenidoCondiciones_ContenidoId_Borrado';
END

-- ContenidoSintomas: index on (ContenidoId, Borrado)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContenidoSintomas_ContenidoId_Borrado' AND object_id = OBJECT_ID('ContenidoSintomas'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ContenidoSintomas_ContenidoId_Borrado]
        ON [ContenidoSintomas] ([ContenidoId], [Borrado])
        INCLUDE ([SintomaId]);
    PRINT 'Created IX_ContenidoSintomas_ContenidoId_Borrado';
END

-- ContenidoTratamientos: index on (ContenidoId, Borrado)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContenidoTratamientos_ContenidoId_Borrado' AND object_id = OBJECT_ID('ContenidoTratamientos'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ContenidoTratamientos_ContenidoId_Borrado]
        ON [ContenidoTratamientos] ([ContenidoId], [Borrado])
        INCLUDE ([TratamientoId]);
    PRINT 'Created IX_ContenidoTratamientos_ContenidoId_Borrado';
END

-- ContenidosPreguntasRelacion: index on (ContenidoId, Borrado)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContenidosPreguntasRelacion_ContenidoId_Borrado' AND object_id = OBJECT_ID('ContenidosPreguntasRelacion'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ContenidosPreguntasRelacion_ContenidoId_Borrado]
        ON [ContenidosPreguntasRelacion] ([ContenidoId], [Borrado])
        INCLUDE ([PreguntaId]);
    PRINT 'Created IX_ContenidosPreguntasRelacion_ContenidoId_Borrado';
END

-- ContenidosCategoriasRelacion: index on (IdContenido, Borrado)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContenidosCategoriasRelacion_IdContenido_Borrado' AND object_id = OBJECT_ID('ContenidosCategoriasRelacion'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ContenidosCategoriasRelacion_IdContenido_Borrado]
        ON [ContenidosCategoriasRelacion] ([IdContenido], [Borrado])
        INCLUDE ([IdCategoria]);
    PRINT 'Created IX_ContenidosCategoriasRelacion_IdContenido_Borrado';
END

PRINT 'All performance indexes verified/created.';
