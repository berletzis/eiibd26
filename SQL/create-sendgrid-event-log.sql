-- =============================================================
-- Script: SendGridEventLog
-- Captura eventos del webhook de SendGrid (delivered, open, click,
-- bounce, dropped, spamreport, unsubscribe).
-- Los custom args (userId, campana, fase, envio_ts) se inyectan
-- al enviar y SendGrid los devuelve en cada evento para casar.
-- =============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'dbo.SendGridEventLog') AND type = 'U'
)
BEGIN
    CREATE TABLE dbo.SendGridEventLog (
        Id              INT IDENTITY(1,1)   NOT NULL,
        -- Tipo y timing del evento
        EventType       NVARCHAR(50)        NOT NULL,   -- 'delivered','open','click','bounce','dropped','spamreport','unsubscribe'
        [Timestamp]     DATETIME2           NOT NULL,   -- unix epoch convertido a DATETIME2
        SgMessageId     NVARCHAR(200)       NULL,       -- sg_message_id de SendGrid
        Email           NVARCHAR(256)       NOT NULL,
        -- Custom args inyectados al enviar (F2 los lee con estos nombres exactos)
        UserId          NVARCHAR(36)        NULL,       -- Guid del usuario (key: "userId")
        FaseLog         INT                 NULL,       -- FaseLog de la campaña (key: "fase")
        CampanaCodigo   NVARCHAR(50)        NULL,       -- Código de campaña (key: "campana"), ej. "reactivacion", "general"
        TemplateId      NVARCHAR(200)       NULL,       -- Template SendGrid (key: "templateId"), útil para campaña general
        EnvioTimestamp  NVARCHAR(40)        NULL,       -- ISO 8601 del momento del envío (key: "envio_ts")
        -- Datos específicos por tipo de evento
        Url             NVARCHAR(500)       NULL,       -- para 'click'
        Reason          NVARCHAR(500)       NULL,       -- para 'bounce' y 'dropped'
        BounceType      NVARCHAR(50)        NULL,       -- 'bounce' | 'blocked' para bounces
        -- Auditoría
        FechaRegistro   DATETIME2           NOT NULL    DEFAULT GETUTCDATE(),
        RawJson         NVARCHAR(MAX)       NULL,       -- evento completo (debug / futuro)

        CONSTRAINT PK_SendGridEventLog PRIMARY KEY CLUSTERED (Id ASC)
    );

    CREATE INDEX IX_SendGridEventLog_Email        ON dbo.SendGridEventLog (Email);
    CREATE INDEX IX_SendGridEventLog_UserId       ON dbo.SendGridEventLog (UserId);
    CREATE INDEX IX_SendGridEventLog_EventType    ON dbo.SendGridEventLog (EventType);
    CREATE INDEX IX_SendGridEventLog_SgMessageId  ON dbo.SendGridEventLog (SgMessageId);
    CREATE INDEX IX_SendGridEventLog_CampanaCodigo ON dbo.SendGridEventLog (CampanaCodigo);

    PRINT 'Tabla SendGridEventLog creada correctamente.';
END
ELSE
BEGIN
    PRINT 'Tabla SendGridEventLog ya existe — sin cambios.';
END
