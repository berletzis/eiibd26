USE eiibd26;
GO
-- 1. Insertar validador_terminos si falta (Nivel 5 paralelo, Orden 5)
IF NOT EXISTS (SELECT 1 FROM MedicoBadge WHERE Codigo = 'validador_terminos')
BEGIN
    INSERT INTO MedicoBadge (Codigo, Nombre, Descripcion, ComoObtenerlo, Icono, Nivel, Orden, Activo)
    VALUES ('validador_terminos', N'Validador de Términos',
            N'Ha validado 3 o más términos del glosario médico.',
            N'Validar 3 o más términos del glosario', 'bi-check-circle-fill', 5, 5, 1);
    PRINT 'Badge validador_terminos insertado.';
END
ELSE PRINT 'validador_terminos ya existe.';
GO
-- 2. Corregir validador_contenido: descripción equivocada (decía glosario) + Orden 6
UPDATE MedicoBadge
SET Nombre        = N'Validador de Contenido',
    Descripcion   = N'Ha validado 3 o más contenidos profesionales.',
    ComoObtenerlo = N'Validar 3 o más contenidos profesionales',
    Icono         = 'bi-check2-circle',
    Nivel         = 5,
    Orden         = 6
WHERE Codigo = 'validador_contenido';
GO
-- 3. creador_contenido: asegurar Nivel 6, Orden 7
UPDATE MedicoBadge SET Nivel = 6, Orden = 7 WHERE Codigo = 'creador_contenido';
GO
