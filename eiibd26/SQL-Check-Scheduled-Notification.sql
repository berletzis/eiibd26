-- Ejecuta este SQL en tu base de datos para ver la notificación

SELECT 
    Id,
    Title,
    Body,
    IsSent,
    ScheduledFor,
    CreatedAt,
    SentAt,
    TotalSent,
    TotalFailed,
    TargetUserIds
FROM PushNotifications
WHERE ScheduledFor IS NOT NULL
ORDER BY CreatedAt DESC;

-- También verifica la hora del servidor SQL
SELECT GETDATE() as HoraServidorSQL, GETUTCDATE() as HoraUTC;
