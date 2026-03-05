-- ===== CREAR USUARIO SISTEMA PARA RESPUESTAS DE IA =====
-- Este script crea el usuario que será el "autor" de las respuestas de IA

DECLARE @SystemUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @SystemEmail NVARCHAR(256) = 'sistema-ia@eiibd.com';
DECLARE @SystemUserName NVARCHAR(256) = 'AsistenteIA';

-- 1. Verificar si ya existe
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = @SystemEmail)
BEGIN
    -- 2. Insertar usuario en AspNetUsers
    INSERT INTO [AspNetUsers] (
        [Id],
        [UserName],
        [NormalizedUserName],
        [Email],
        [NormalizedEmail],
        [EmailConfirmed],
        [PasswordHash],
        [SecurityStamp],
        [ConcurrencyStamp],
        [PhoneNumberConfirmed],
        [TwoFactorEnabled],
        [LockoutEnabled],
        [AccessFailedCount]
    )
    VALUES (
        @SystemUserId,
        @SystemUserName,
        UPPER(@SystemUserName),
        @SystemEmail,
        UPPER(@SystemEmail),
        1, -- EmailConfirmed
        'SYSTEM_NO_LOGIN', -- PasswordHash (no puede iniciar sesión)
        NEWID(), -- SecurityStamp
        NEWID(), -- ConcurrencyStamp
        0, -- PhoneNumberConfirmed
        0, -- TwoFactorEnabled
        1, -- LockoutEnabled
        0  -- AccessFailedCount
    );

    -- 3. Crear perfil básico
    INSERT INTO [Perfil] (
        [idUser],
        [Nombre],
        [Avatar],
        [FechaCreacion]
    )
    VALUES (
        @SystemUserId,
        'Asistente Educativo IA',
        '/img/ai-avatar.png', -- Asegúrate de tener esta imagen
        GETUTCDATE()
    );

    -- 4. Mostrar el GUID generado
    SELECT 
        'Usuario Sistema creado exitosamente' AS Mensaje,
        @SystemUserId AS SystemUserId,
        'Copia este GUID y úsalo en appsettings.json -> AiAnswer:SystemUserId' AS Instruccion;
END
ELSE
BEGIN
    -- Si ya existe, mostrar el GUID existente
    SELECT 
        'Usuario Sistema ya existe' AS Mensaje,
        Id AS SystemUserId,
        'Usa este GUID en appsettings.json -> AiAnswer:SystemUserId' AS Instruccion
    FROM AspNetUsers 
    WHERE Email = @SystemEmail;
END

-- ===== FIN SCRIPT USUARIO SISTEMA =====
