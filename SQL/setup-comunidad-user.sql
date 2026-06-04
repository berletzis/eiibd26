-- ===== CREAR USUARIO COMUNIDAD EIIBD =====
-- Script idempotente. Ejecutar directo en SQL Server.
-- Al terminar, copiar el GUID devuelto a appsettings -> Comunidad:UserId

DECLARE @ComunidadId   UNIQUEIDENTIFIER = NEWID();
DECLARE @ComunidadEmail     NVARCHAR(256) = 'comunidad@eiibd.com';
DECLARE @ComunidadUserName  NVARCHAR(256) = 'ComunidadEIIBD';

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = @ComunidadEmail)
BEGIN
    INSERT INTO [AspNetUsers] (
        [Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail],
        [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
        [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount]
    )
    VALUES (
        @ComunidadId,
        @ComunidadUserName,
        UPPER(@ComunidadUserName),
        @ComunidadEmail,
        UPPER(@ComunidadEmail),
        1,                  -- EmailConfirmed
        'SYSTEM_NO_LOGIN',  -- PasswordHash: bloquea login
        NEWID(),            -- SecurityStamp
        NEWID(),            -- ConcurrencyStamp
        0, 0, 1, 0
    );

    -- idZone = NULL explícito para evitar DEFAULT constraint -> FK_Perfil_TimeZones
    INSERT INTO [Perfil] ([idUser], [Nombre], [Avatar], [FechaCreacion], [idZone])
    VALUES (@ComunidadId, 'Comunidad EIIBD', '/img/avatar-placeholder.png', GETUTCDATE(), NULL);

    SELECT
        'Usuario Comunidad EIIBD creado' AS Mensaje,
        @ComunidadId                     AS ComunidadUserId,
        'Copiar GUID arriba -> appsettings.json -> Comunidad:UserId' AS Instruccion;
END
ELSE
BEGIN
    SELECT
        'Ya existe' AS Mensaje,
        Id          AS ComunidadUserId,
        'Copiar GUID arriba -> appsettings.json -> Comunidad:UserId' AS Instruccion
    FROM AspNetUsers WHERE Email = @ComunidadEmail;
END
-- ===== FIN SCRIPT COMUNIDAD =====
