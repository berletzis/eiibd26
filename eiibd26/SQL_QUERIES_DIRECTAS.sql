-- ============================================
-- SQL QUERIES DIRECTAS - Ejecutar en SQL Server
-- ============================================

-- ============================================
-- PASO 1: Agregar campos a tabla SINTOMAS
-- ============================================

ALTER TABLE dbo.sintomas ADD 
    DescripcionIA NVARCHAR(MAX) NULL,
    ValidadoIA BIT DEFAULT 0,
    ValidadoHumano BIT DEFAULT 0,
    RelacionEII NVARCHAR(255) NULL,
    FechaActualizacionIA DATETIME NULL;

-- Crear índices para mejorar performance
CREATE INDEX IX_sintomas_ValidadoIA ON dbo.sintomas(ValidadoIA);
CREATE INDEX IX_sintomas_FechaActualizacionIA ON dbo.sintomas(FechaActualizacionIA);

-- Verificar que se agregaron correctamente
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'sintomas' 
ORDER BY ORDINAL_POSITION DESC;

-- ============================================
-- PASO 2: Agregar campos a tabla TRATAMIENTOS
-- ============================================

ALTER TABLE dbo.tratamientos ADD 
    DescripcionIA NVARCHAR(MAX) NULL,
    ValidadoIA BIT DEFAULT 0,
    ValidadoHumano BIT DEFAULT 0,
    RelacionEII NVARCHAR(255) NULL,
    FechaActualizacionIA DATETIME NULL;

-- Crear índices para mejorar performance
CREATE INDEX IX_tratamientos_ValidadoIA ON dbo.tratamientos(ValidadoIA);
CREATE INDEX IX_tratamientos_FechaActualizacionIA ON dbo.tratamientos(FechaActualizacionIA);

-- Verificar que se agregaron correctamente
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'tratamientos' 
ORDER BY ORDINAL_POSITION DESC;

-- ============================================
-- PASO 3: Crear tabla SINTOMASNOTAS
-- ============================================

CREATE TABLE dbo.SintomasNotas (
    id INT PRIMARY KEY IDENTITY(1,1),
    SintomaId INT NOT NULL,
    UsuarioId UNIQUEIDENTIFIER NULL,
    Nota NVARCHAR(MAX) NOT NULL,
    EsNotaIA BIT DEFAULT 0,
    FechaCreado DATETIME DEFAULT GETUTCDATE(),
    FechaModificado DATETIME DEFAULT GETUTCDATE(),
    Eliminado BIT DEFAULT 0,
    
    -- Foreign Keys
    CONSTRAINT FK_SintomasNotas_Sintomas 
        FOREIGN KEY (SintomaId) 
        REFERENCES dbo.sintomas(id) 
        ON DELETE CASCADE,
    
    CONSTRAINT FK_SintomasNotas_Usuario 
        FOREIGN KEY (UsuarioId) 
        REFERENCES dbo.AspNetUsers(Id) 
        ON DELETE SET NULL
);

-- Crear índices
CREATE INDEX IX_SintomasNotas_SintomaId ON dbo.SintomasNotas(SintomaId);
CREATE INDEX IX_SintomasNotas_UsuarioId ON dbo.SintomasNotas(UsuarioId);
CREATE INDEX IX_SintomasNotas_EsNotaIA ON dbo.SintomasNotas(EsNotaIA);
CREATE INDEX IX_SintomasNotas_Eliminado ON dbo.SintomasNotas(Eliminado);
CREATE INDEX IX_SintomasNotas_FechaCreado ON dbo.SintomasNotas(FechaCreado);

-- Verificar que se creó
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'SintomasNotas';

-- ============================================
-- PASO 4: Crear tabla TRATAMIENTOSNOTAS
-- ============================================

CREATE TABLE dbo.TratamientosNotas (
    id INT PRIMARY KEY IDENTITY(1,1),
    TratamientoId INT NOT NULL,
    UsuarioId UNIQUEIDENTIFIER NULL,
    Nota NVARCHAR(MAX) NOT NULL,
    EsNotaIA BIT DEFAULT 0,
    FechaCreado DATETIME DEFAULT GETUTCDATE(),
    FechaModificado DATETIME DEFAULT GETUTCDATE(),
    Eliminado BIT DEFAULT 0,
    
    -- Foreign Keys
    CONSTRAINT FK_TratamientosNotas_Tratamientos 
        FOREIGN KEY (TratamientoId) 
        REFERENCES dbo.tratamientos(id) 
        ON DELETE CASCADE,
    
    CONSTRAINT FK_TratamientosNotas_Usuario 
        FOREIGN KEY (UsuarioId) 
        REFERENCES dbo.AspNetUsers(Id) 
        ON DELETE SET NULL
);

-- Crear índices
CREATE INDEX IX_TratamientosNotas_TratamientoId ON dbo.TratamientosNotas(TratamientoId);
CREATE INDEX IX_TratamientosNotas_UsuarioId ON dbo.TratamientosNotas(UsuarioId);
CREATE INDEX IX_TratamientosNotas_EsNotaIA ON dbo.TratamientosNotas(EsNotaIA);
CREATE INDEX IX_TratamientosNotas_Eliminado ON dbo.TratamientosNotas(Eliminado);
CREATE INDEX IX_TratamientosNotas_FechaCreado ON dbo.TratamientosNotas(FechaCreado);

-- Verificar que se creó
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'TratamientosNotas';

-- ============================================
-- PASO 5: Verificar Integridad Referencial
-- ============================================

-- Ver todas las relaciones de SintomasNotas y TratamientosNotas
SELECT 
    CONSTRAINT_NAME = rc.CONSTRAINT_NAME,
    TABLE_NAME = kcu.TABLE_NAME,
    COLUMN_NAME = kcu.COLUMN_NAME,
    REFERENCED_TABLE_NAME = ccu.TABLE_NAME,
    REFERENCED_COLUMN = ccu.COLUMN_NAME
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS rc
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE AS ccu
    ON rc.UNIQUE_CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
WHERE kcu.TABLE_NAME IN ('SintomasNotas', 'TratamientosNotas')
ORDER BY kcu.TABLE_NAME;

-- ============================================
-- PASO 6: Poblamiento de Datos de Prueba (OPCIONAL)
-- ============================================

-- Agregar una nota de prueba a SintomasNotas
INSERT INTO dbo.SintomasNotas (SintomaId, Nota, EsNotaIA, FechaCreado, FechaModificado)
SELECT TOP 1 
    id, 
    'Esta es una nota de prueba',
    0,
    GETUTCDATE(),
    GETUTCDATE()
FROM dbo.sintomas
WHERE id > 0;

-- Verificar que se insertó
SELECT * FROM dbo.SintomasNotas ORDER BY FechaCreado DESC;

-- ============================================
-- PASO 7: Rollback (EN CASO DE NECESARIO)
-- ============================================

-- SOLO si necesitas deshacer los cambios:
/*
-- Eliminar tablas nuevas
DROP TABLE IF EXISTS dbo.TratamientosNotas;
DROP TABLE IF EXISTS dbo.SintomasNotas;

-- Eliminar columnas de sintomas
ALTER TABLE dbo.sintomas DROP COLUMN DescripcionIA;
ALTER TABLE dbo.sintomas DROP COLUMN ValidadoIA;
ALTER TABLE dbo.sintomas DROP COLUMN ValidadoHumano;
ALTER TABLE dbo.sintomas DROP COLUMN RelacionEII;
ALTER TABLE dbo.sintomas DROP COLUMN FechaActualizacionIA;

-- Eliminar columnas de tratamientos
ALTER TABLE dbo.tratamientos DROP COLUMN DescripcionIA;
ALTER TABLE dbo.tratamientos DROP COLUMN ValidadoIA;
ALTER TABLE dbo.tratamientos DROP COLUMN ValidadoHumano;
ALTER TABLE dbo.tratamientos DROP COLUMN RelacionEII;
ALTER TABLE dbo.tratamientos DROP COLUMN FechaActualizacionIA;
*/

-- ============================================
-- PASO 8: Validación Final
-- ============================================

-- Verificar que las columnas existen en sintomas
IF COL_LENGTH('dbo.sintomas', 'DescripcionIA') IS NOT NULL
BEGIN
    SELECT 
        Tabla = 'sintomas', 
        Total = COUNT(*),
        ConDescripcionIA = SUM(CASE WHEN DescripcionIA IS NOT NULL THEN 1 ELSE 0 END),
        ValidadoIA_Count = SUM(CASE WHEN ValidadoIA = 1 THEN 1 ELSE 0 END),
        ValidadoHumano_Count = SUM(CASE WHEN ValidadoHumano = 1 THEN 1 ELSE 0 END)
    FROM dbo.sintomas
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA: Las columnas aún no se han agregado a sintomas. Verifica que los pasos 1 y 2 se ejecutaron sin errores.'
END

-- Verificar que las columnas existen en tratamientos
IF COL_LENGTH('dbo.tratamientos', 'DescripcionIA') IS NOT NULL
BEGIN
    SELECT 
        Tabla = 'tratamientos', 
        Total = COUNT(*),
        ConDescripcionIA = SUM(CASE WHEN DescripcionIA IS NOT NULL THEN 1 ELSE 0 END),
        ValidadoIA_Count = SUM(CASE WHEN ValidadoIA = 1 THEN 1 ELSE 0 END),
        ValidadoHumano_Count = SUM(CASE WHEN ValidadoHumano = 1 THEN 1 ELSE 0 END)
    FROM dbo.tratamientos
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA: Las columnas aún no se han agregado a tratamientos. Verifica que los pasos 1 y 2 se ejecutaron sin errores.'
END

-- Ver estadísticas de notas (si existen las tablas)
IF OBJECT_ID('dbo.SintomasNotas', 'U') IS NOT NULL
BEGIN
    SELECT 
        Tabla = 'SintomasNotas',
        Total = COUNT(*),
        Eliminadas = SUM(CASE WHEN Eliminado = 1 THEN 1 ELSE 0 END),
        NotasIA = SUM(CASE WHEN EsNotaIA = 1 THEN 1 ELSE 0 END)
    FROM dbo.SintomasNotas
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA: La tabla SintomasNotas no existe. Verifica que el paso 3 se ejecutó sin errores.'
END

IF OBJECT_ID('dbo.TratamientosNotas', 'U') IS NOT NULL
BEGIN
    SELECT 
        Tabla = 'TratamientosNotas',
        Total = COUNT(*),
        Eliminadas = SUM(CASE WHEN Eliminado = 1 THEN 1 ELSE 0 END),
        NotasIA = SUM(CASE WHEN EsNotaIA = 1 THEN 1 ELSE 0 END)
    FROM dbo.TratamientosNotas
END
ELSE
BEGIN
    PRINT 'ADVERTENCIA: La tabla TratamientosNotas no existe. Verifica que el paso 4 se ejecutó sin errores.'
END

-- ============================================
-- ✅ LISTO
-- ============================================

-- Si llegaste aquí sin errores, todo está bien
-- Ahora puedes ejecutar las migraciones de EF Core:
-- 
-- Add-Migration AgregaSintomasYTratamientosIA
-- Update-Database
