/* ============================================================================
   seed-notas-clinicas.sql  —  Carga de las 23 notas clínicas (Platillos, F1)
   ----------------------------------------------------------------------------
   Fuente: Documentation/BORRADOR-notas-clinicas-v2.md
   17 grupos con contenido + 6 ingredientes = 23 notas. El grupo "otro" NO se carga.

   CANDADO: TODAS entran con RevisadaPorMedico = 0. NINGUNA se muestra al paciente
   hasta que un médico la apruebe (F2). Verificación tras correr:
       SELECT COUNT(*) FROM PlatNotaClinica WHERE RevisadaPorMedico = 1;  -- debe ser 0

   FALLA FUERTE: el mapeo encabezado→grupo/ingrediente es explícito. Si un destino
   no existe por nombre, el script ABORTA (THROW) y hace ROLLBACK de TODO. Una nota
   que no se cargó y nadie se enteró deja a un paciente sin información que sí
   escribimos — ese fallo silencioso NO se permite aquí.

   Idempotente: cada nota está guardada con IF NOT EXISTS sobre (TipoDestino, DestinoId).
   Re-correr no duplica. Correr DESPUÉS de create-notas-clinicas.sql y seed-platillos.sql.

   ⚠️ ENCODING: el archivo trae acentos y "¿". Cargar con:  sqlcmd -f 65001 -I -i ...
   (si no, mojibake). Ver memoria sqlcmd_codepage_seed.
   ============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRAN;
BEGIN TRY

    DECLARE @nid INT, @did INT;

    /* ======================= GRUPOS ======================= */

    -- ---- Lácteos → grupo 'lácteo' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'lácteo');
    IF @did IS NULL THROW 50001, 'Falta el grupo "lácteo" (borrador: Lácteos). Corre seed-platillos.sql primero. Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer lácteos?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Leche, queso, yogur, crema. Para la mayoría de la gente son la principal fuente de calcio.'),
            (@nid, 2, N'¿Qué suele pasar?', N'- A algunas personas les caen mal por la lactosa: inflamación, gases, diarrea.
- A muchas otras no les hacen absolutamente nada.
- La intolerancia a la lactosa se reporta en una parte importante de quienes viven con EII, sobre todo si está afectado el intestino delgado.'),
            (@nid, 3, N'Antes de eliminarlos', N'No están prohibidos en EII. Y quitarlos "por si acaso" tiene un costo: en EII el riesgo de perder densidad ósea es mayor que en la población general, y los corticoides lo aumentan por su cuenta.
Si sospechas que te caen mal, prueba esto antes de descartarlos:
- Los quesos curados (manchego, parmesano, añejo) casi no tienen lactosa.
- El yogur suele tolerarse mejor que la leche.
- Existe leche sin lactosa.
Y si de verdad tienes que dejarlos, no lo hagas solo: hay que reemplazar el calcio y la vitamina D por otra vía.'),
            (@nid, 4, N'Importante', N'La tolerancia es individual: lo que a otra persona le cae mal, a ti puede caerte bien.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Crohn''s & Colitis Foundation — ¿Qué debo comer?', N'https://www.crohnscolitisfoundation.org/patientsandcaregivers/diet-and-nutrition/what-should-i-eat'),
            (@nid, 2, N'Osteoporosis en EII', N'https://pmc.ncbi.nlm.nih.gov/articles/PMC2894700/');
    END

    -- ---- Mariscos → grupo 'marisco' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'marisco');
    IF @did IS NULL THROW 50001, 'Falta el grupo "marisco" (borrador: Mariscos). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer mariscos?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Camarón, pulpo, almeja, ostión, jaiba.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Bien cocidos, no tienen ninguna contraindicación especial en EII.'),
            (@nid, 3, N'Lo que sí debes saber — y esto no es sobre tolerancia', N'Si tomas inmunosupresores o biológicos, tu defensa contra infecciones está bajada. Los mariscos crudos o poco cocidos —ceviche, aguachile, ostión crudo, sushi— pueden traer bacterias como la listeria, que en personas inmunocomprometidas es grave de verdad.
No es una molestia digestiva. Es un riesgo de infección.
Cómo comerlos con tranquilidad:
- Camarón, callo o jaiba: hasta que la carne quede opaca.
- Almeja, ostión y mejillón: hasta que la concha se abra con el calor.
- Evita lo crudo y los ahumados refrigerados mientras estés bajo tratamiento inmunosupresor.'),
            (@nid, 4, N'Importante', N'Si no tomas inmunosupresores, esto no aplica igual. Pregúntale a tu médico qué tratamiento llevas.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Listeria y seguridad alimentaria en inmunocomprometidos', N'https://pmc.ncbi.nlm.nih.gov/articles/PMC11486915/'),
            (@nid, 2, N'USDA — Listeria monocytogenes', N'https://www.usda.gov/about-usda/news/blog/listeria-monocytogenes-listeriosis-and-you');
    END

    -- ---- Verduras → grupo 'verdura' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'verdura');
    IF @did IS NULL THROW 50001, 'Falta el grupo "verdura" (borrador: Verduras). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer verduras?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Todas: cocidas, crudas, en ensalada, en guisado.'),
            (@nid, 2, N'¿Qué suele pasar?', N'- En brote, la fibra cruda —cáscaras, tallos, hojas duras, semillas— puede irritar.
- En remisión, la mayoría las tolera sin problema.
- Mucha gente las elimina después de un brote… y ya nunca las vuelve a comer. Ese es el error.'),
            (@nid, 3, N'Lo que casi siempre funciona', N'El problema rara vez es la verdura. Es cómo viene preparada.
- Cocínalas, pélalas, quítales las semillas.
- Después de un brote, reintrodúcelas poco a poco.
- En remisión, la recomendación es la contraria a eliminarlas: dieta lo más amplia posible.'),
            (@nid, 4, N'Importante', N'Eliminarlas de forma permanente te cuesta fibra, vitaminas y variedad.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Crohn''s & Colitis Foundation — ¿Qué debo comer?', N'https://www.crohnscolitisfoundation.org/patientsandcaregivers/diet-and-nutrition/what-should-i-eat'),
            (@nid, 2, N'ESPEN 2023', N'https://www.clinicalnutritionjournal.com/article/S0261-5614(22)00428-9/fulltext');
    END

    -- ---- Frutas → grupo 'fruta' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'fruta');
    IF @did IS NULL THROW 50001, 'Falta el grupo "fruta" (borrador: Frutas). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer frutas?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Frescas, cocidas, en jugo o en compota.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Igual que las verduras: el problema no suele ser la fruta, sino su fibra — cáscara, semillas, hollejo.'),
            (@nid, 3, N'Cómo llevarlas', N'- En brote: pelada, sin semillas, o cocida (compota, plátano, manzana cocida).
- En remisión: sin restricción.'),
            (@nid, 4, N'Importante', N'Eliminar la fruta de forma permanente después de un brote es un error común, y te cuesta vitaminas.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Crohn''s & Colitis Foundation — ¿Qué debo comer?', N'https://www.crohnscolitisfoundation.org/patientsandcaregivers/diet-and-nutrition/what-should-i-eat');
    END

    -- ---- Cereales → grupo 'cereal' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'cereal');
    IF @did IS NULL THROW 50001, 'Falta el grupo "cereal" (borrador: Cereales). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer cereales?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Arroz, pan, pasta, tortilla, avena.'),
            (@nid, 2, N'¿Qué suele pasar?', N'- En brote, los refinados (arroz blanco, pan blanco, pasta) caen más suave que los integrales.
- En remisión, los integrales aportan fibra que le hace bien a tu intestino.'),
            (@nid, 3, N'Un aviso que casi nadie te da', N'Mucha gente con EII deja el gluten sin necesidad.
Si no tienes celiaquía ni una sensibilidad comprobada, no hay razón para hacerlo. Una dieta sin gluten innecesaria es cara, restrictiva, y no va a mejorar tu enfermedad. Las guías son claras: las dietas de exclusión, en general, no se recomiendan.'),
            (@nid, 4, N'Importante', N'Quitar un grupo entero de alimentos siempre tiene un costo. Que sea con una razón.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'ESPEN 2023', N'https://www.clinicalnutritionjournal.com/article/S0261-5614(22)00428-9/fulltext'),
            (@nid, 2, N'CCF — Dietas especiales en EII', N'https://www.crohnscolitisfoundation.org/patientandcaregivers/diet-and-nutrition/special-ibd-diets');
    END

    -- ---- Carne (roja) → grupo 'carne' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'carne');
    IF @did IS NULL THROW 50001, 'Falta el grupo "carne" (borrador: Carne roja). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer carne roja?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué es?', N'Res, cerdo, cordero.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Aporta proteína y hierro, y ambos importan mucho en EII — sobre todo si tienes anemia o has perdido peso.
En brote, los cortes grasos suelen digerirse peor. Eso es cuestión de grasa y preparación, no de la carne en sí.'),
            (@nid, 3, N'Lo que dice la evidencia (y lo que NO dice)', N'Un metaanálisis grande encontró que comer mucha carne roja se asocia con desarrollar colitis ulcerosa. Pero también encontró algo importante para ti: no se asocia con recaídas en quien ya tiene la enfermedad.
Traducido: si ya vives con EII, no hay evidencia sólida de que la carne roja te provoque un brote.'),
            (@nid, 4, N'Cómo llevarla', N'Cortes magros, bien cocida, porciones moderadas. Eso es buena alimentación en general — no una restricción por tu enfermedad.'),
            (@nid, 5, N'Importante', N'Quitar la proteína tiene su propio costo, sobre todo si estás perdiendo masa muscular.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Carne roja y procesada en colitis ulcerosa — metaanálisis 2025', N'https://pmc.ncbi.nlm.nih.gov/articles/PMC12463588/');
    END

    -- ---- Embutidos → grupo 'embutido' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'embutido');
    IF @did IS NULL THROW 50001, 'Falta el grupo "embutido" (borrador: Embutidos). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer embutidos?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Tocino, salchicha, jamón, chorizo.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Son altos en grasa y sal, y eso puede sentarte pesado — sobre todo en brote.'),
            (@nid, 3, N'Lo que dice la evidencia (y lo que NO dice)', N'El mismo metaanálisis encontró que los embutidos muestran una tendencia no significativa al riesgo de desarrollar colitis ulcerosa, y no se asocian con recaídas en quien ya la tiene.
O sea: no hay evidencia sólida de que te provoquen un brote.'),
            (@nid, 4, N'Cómo llevarlos', N'Moderarlos es buen consejo de alimentación general —por la grasa y el sodio—, pero no es una restricción que te imponga tu enfermedad. No tienes que eliminarlos por tener EII.'),
            (@nid, 5, N'Importante', N'Si te caen pesados, es por la grasa. Prueba porciones más chicas antes de descartarlos.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Carne roja y procesada en colitis ulcerosa — metaanálisis 2025', N'https://pmc.ncbi.nlm.nih.gov/articles/PMC12463588/');
    END

    -- ---- Ave → grupo 'ave' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'ave');
    IF @did IS NULL THROW 50001, 'Falta el grupo "ave" (borrador: Ave). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer ave?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué es?', N'Pollo, pavo.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Suele tolerarse muy bien. Es proteína magra, y de las carnes que mejor caen incluso en brote, si se prepara sin grasa añadida.'),
            (@nid, 3, N'Importante', N'No hay motivo para restringirla. Bien cocida, es de las opciones más seguras y nutritivas que tienes.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Crohn''s & Colitis Foundation — ¿Qué debo comer?', N'https://www.crohnscolitisfoundation.org/patientsandcaregivers/diet-and-nutrition/what-should-i-eat');
    END

    -- ---- Pescado → grupo 'pescado' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'pescado');
    IF @did IS NULL THROW 50001, 'Falta el grupo "pescado" (borrador: Pescado). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer pescado?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué es?', N'Blanco o azul, fresco o enlatado.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Suele tolerarse bien y es buena fuente de proteína. Cocido y sin empanizar, es de las opciones más amables.'),
            (@nid, 3, N'Ojo', N'Si tomas inmunosupresores, aplica lo mismo que con los mariscos: evita el pescado crudo (sushi, sashimi, ceviche) por riesgo de infección, no de digestión.'),
            (@nid, 4, N'Importante', N'No hay razón para restringirlo. Bien cocido, adelante.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Listeria en inmunocomprometidos', N'https://pmc.ncbi.nlm.nih.gov/articles/PMC11486915/');
    END

    -- ---- Huevo → grupo 'huevo' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'huevo');
    IF @did IS NULL THROW 50001, 'Falta el grupo "huevo" (borrador: Huevo). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer huevo?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué es?', N'Entero, clara o yema.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Suele tolerarse bien y es proteína de buena calidad — importante si has perdido peso o masa muscular.'),
            (@nid, 3, N'Ojo', N'Si tomas inmunosupresores, evítalo crudo o poco cocido (mayonesa casera, huevo tibio, postres sin cocer). Bien cocido, sin problema.'),
            (@nid, 4, N'Importante', N'No hay razón para evitarlo, salvo alergia o intolerancia comprobada.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'USDA — Listeria monocytogenes', N'https://www.usda.gov/about-usda/news/blog/listeria-monocytogenes-listeriosis-and-you');
    END

    -- ---- Frutos secos → grupo 'fruto-seco' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'fruto-seco');
    IF @did IS NULL THROW 50001, 'Falta el grupo "fruto-seco" (borrador: Frutos secos). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer frutos secos?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Nuez, almendra, cacahuate, pistache, semillas.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Tienen fibra dura y pueden sentirse mal en brote.'),
            (@nid, 3, N'La recomendación vieja que ya no aplica', N'Durante años se dijo que había que evitarlos para siempre. Eso ya no se sostiene. Las guías actuales recomiendan que, en remisión, la dieta sea lo más amplia posible — e incluyen explícitamente nueces y semillas entre lo que conviene comer.'),
            (@nid, 4, N'Cómo llevarlos', N'- En brote: pueden molestar. Las cremas (de cacahuate, de almendra) son una alternativa más suave.
- En remisión: pruébalos. No hay razón para tenerles miedo.'),
            (@nid, 5, N'Importante', N'Si te caen mal, no los comas. Pero no los elimines de por vida por una recomendación vieja.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Crohn''s & Colitis Foundation — ¿Qué debo comer?', N'https://www.crohnscolitisfoundation.org/patientsandcaregivers/diet-and-nutrition/what-should-i-eat');
    END

    -- ---- Legumbres → grupo 'legumbre' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'legumbre');
    IF @did IS NULL THROW 50001, 'Falta el grupo "legumbre" (borrador: Legumbres). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer legumbres?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Frijol, lenteja, garbanzo, haba.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Suelen dar gas e hinchazón. En brote, muchos no las toleran.'),
            (@nid, 3, N'Antes de eliminarlas', N'Son una fuente excelente de proteína y fibra, y quitarlas para siempre es perder mucho. Prueba en remisión:
- Bien cocidas y sin cáscara.
- Porciones chicas.
- En puré (como el hummus), que suele caer mejor.'),
            (@nid, 4, N'Importante', N'Que te caigan mal en brote no significa que no las puedas comer nunca.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Crohn''s & Colitis Foundation — ¿Qué debo comer?', N'https://www.crohnscolitisfoundation.org/patientsandcaregivers/diet-and-nutrition/what-should-i-eat');
    END

    -- ---- Tubérculos → grupo 'tubérculo' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'tubérculo');
    IF @did IS NULL THROW 50001, 'Falta el grupo "tubérculo" (borrador: Tubérculos). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer tubérculos?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Papa, camote, yuca.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Cocidos y pelados suelen tolerarse muy bien. Son de los alimentos más amables en brote y buena fuente de energía.'),
            (@nid, 3, N'Importante', N'Salvo que te caigan mal, no hay motivo para evitarlos.');
    END

    -- ---- Hongos → grupo 'hongo' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'hongo');
    IF @did IS NULL THROW 50001, 'Falta el grupo "hongo" (borrador: Hongos). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer hongos?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Champiñón, seta, portobello.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Tienen una fibra que a algunas personas les cuesta digerir. No hay contraindicación específica en EII.'),
            (@nid, 3, N'Importante', N'Si te caen bien, no hay razón para evitarlos.');
    END

    -- ---- Grasas y aceites → grupo 'grasa' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'grasa');
    IF @did IS NULL THROW 50001, 'Falta el grupo "grasa" (borrador: Grasas y aceites). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer grasas y aceites?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Aceite, mantequilla, mayonesa, aguacate.'),
            (@nid, 2, N'¿Qué suele pasar?', N'La grasa no es el enemigo: es esencial y aporta calorías, algo que importa si estás perdiendo peso.
Lo que suele caer mal no es la grasa en sí, sino la fritura y las cantidades grandes de golpe. El aceite de oliva se tolera bien.'),
            (@nid, 3, N'Cuándo sí hay una razón concreta', N'Si tuviste cirugía de intestino delgado, o notas diarrea justo después de comer grasa, coméntalo con tu médico: ahí puede haber un motivo real detrás, y tiene manejo.'),
            (@nid, 4, N'Importante', N'En brote suele recomendarse bajarle a la grasa. Eso es temporal, no permanente.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Crohn''s & Colitis Foundation — ¿Qué debo comer?', N'https://www.crohnscolitisfoundation.org/patientsandcaregivers/diet-and-nutrition/what-should-i-eat');
    END

    -- ---- Condimentos → grupo 'condimento' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'condimento');
    IF @did IS NULL THROW 50001, 'Falta el grupo "condimento" (borrador: Condimentos). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Puedo comer condimentos?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Sal, pimienta, hierbas, especias.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Las hierbas y especias suaves suelen ir bien en cantidades normales. El picante es otro tema y está marcado aparte.'),
            (@nid, 3, N'Importante', N'No hay razón para comer sin sabor.');
    END

    -- ---- Bebidas → grupo 'bebida' ----
    SET @did = (SELECT Id FROM PlatGrupo WHERE Nombre = N'bebida');
    IF @did IS NULL THROW 50001, 'Falta el grupo "bebida" (borrador: Bebidas). Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Grupo' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Grupo', @did, N'¿Qué puedo beber?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué son?', N'Agua, café, refresco, jugos, alcohol.'),
            (@nid, 2, N'¿Qué suele pasar?', N'Mantenerte hidratado es especialmente importante en EII: la diarrea te deshidrata más rápido de lo que crees.
- El agua es lo mejor.
- Las bebidas con gas pueden darte más inflamación y molestia.
- El café y el alcohol están marcados aparte.'),
            (@nid, 3, N'Importante', N'Si estás en brote con muchas evacuaciones, la hidratación no es opcional. Pregúntale a tu médico si te conviene un suero de rehidratación.');
    END

    /* ======================= INGREDIENTES ======================= */

    -- ---- Queso → ingrediente 'queso' ----
    SET @did = (SELECT Id FROM PlatIngrediente WHERE Nombre = N'queso');
    IF @did IS NULL THROW 50002, 'Falta el ingrediente "queso". Corre seed-platillos.sql primero. Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Ingrediente' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Ingrediente', @did, N'¿Puedo comer queso?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué es?', N'El ejemplo perfecto de por qué "los lácteos me caen mal" casi nunca es toda la verdad.'),
            (@nid, 2, N'¿Qué suele pasar?', N'No todos los quesos son iguales, y la diferencia está en la lactosa:
- Los curados —manchego, parmesano, añejo— casi no tienen lactosa.
- Los frescos —panela, requesón, cottage— tienen más.'),
            (@nid, 3, N'Antes de descartar el queso entero', N'Si te cayó mal un queso fresco, eso no significa que no puedas comer queso. Prueba uno curado antes de eliminarlos todos.
Es la diferencia entre perder un alimento y perder una categoría completa.'),
            (@nid, 4, N'Importante', N'Si de todos modos te caen mal todos, revisa con tu nutriólogo cómo vas a cubrir el calcio.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Crohn''s & Colitis Foundation — ¿Qué debo comer?', N'https://www.crohnscolitisfoundation.org/patientsandcaregivers/diet-and-nutrition/what-should-i-eat');
    END

    -- ---- Leche → ingrediente 'leche' ----
    SET @did = (SELECT Id FROM PlatIngrediente WHERE Nombre = N'leche');
    IF @did IS NULL THROW 50002, 'Falta el ingrediente "leche". Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Ingrediente' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Ingrediente', @did, N'¿Puedo tomar leche?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué suele pasar?', N'Es el lácteo que más lactosa tiene, así que es el que más seguido molesta.'),
            (@nid, 2, N'Antes de dejarla', N'Existe leche sin lactosa, que sabe igual y te deja el calcio. Dejar la leche no tiene por qué significar dejar los lácteos.');
    END

    -- ---- Yogur → ingrediente 'yogur' ----
    SET @did = (SELECT Id FROM PlatIngrediente WHERE Nombre = N'yogur');
    IF @did IS NULL THROW 50002, 'Falta el ingrediente "yogur". Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Ingrediente' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Ingrediente', @did, N'¿Puedo comer yogur?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué suele pasar?', N'Suele tolerarse mejor que la leche: sus cultivos ya digirieron parte de la lactosa.'),
            (@nid, 2, N'Importante', N'Si la leche te cae mal, el yogur es lo primero que vale la pena probar antes de rendirte con los lácteos.');
    END

    -- ---- Cebolla → ingrediente 'cebolla' ----
    SET @did = (SELECT Id FROM PlatIngrediente WHERE Nombre = N'cebolla');
    IF @did IS NULL THROW 50002, 'Falta el ingrediente "cebolla". Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Ingrediente' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Ingrediente', @did, N'¿Puedo comer cebolla?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué suele pasar?', N'Cruda irrita a mucha gente. Cocida suele tolerarse bien.'),
            (@nid, 2, N'Antes de eliminarla', N'Prueba cocida antes de sacarla de tu vida. Es de los casos donde la preparación cambia todo.');
    END

    -- ---- Leche de coco → ingrediente 'leche de coco' ----
    SET @did = (SELECT Id FROM PlatIngrediente WHERE Nombre = N'leche de coco');
    IF @did IS NULL THROW 50002, 'Falta el ingrediente "leche de coco". Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Ingrediente' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Ingrediente', @did, N'¿Puedo tomar leche de coco?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué es?', N'No es un lácteo, aunque el nombre confunda. No tiene lactosa.'),
            (@nid, 2, N'Para qué sirve', N'Es justamente la alternativa que usa mucha gente que no tolera los lácteos. Si estás evitando la lactosa, esta no es tu enemiga.');
    END

    -- ---- Camarón → ingrediente 'camarón' ----
    SET @did = (SELECT Id FROM PlatIngrediente WHERE Nombre = N'camarón');
    IF @did IS NULL THROW 50002, 'Falta el ingrediente "camarón". Abortado.', 1;
    IF NOT EXISTS (SELECT 1 FROM PlatNotaClinica WHERE TipoDestino = 'Ingrediente' AND DestinoId = @did)
    BEGIN
        INSERT PlatNotaClinica(TipoDestino, DestinoId, Titulo, RevisadaPorMedico, Activo, FechaCreacion)
            VALUES('Ingrediente', @did, N'¿Puedo comer camarón?', 0, 1, SYSUTCDATETIME());
        SET @nid = SCOPE_IDENTITY();
        INSERT PlatNotaSeccion(NotaClinicaId, Orden, Titulo, Contenido) VALUES
            (@nid, 1, N'¿Qué suele pasar?', N'Bien cocido, no hay problema.'),
            (@nid, 2, N'Ojo — esto es de seguridad, no de tolerancia', N'Crudo (ceviche, aguachile) sí importa: si tomas inmunosupresores o biológicos, tu riesgo de infección es mayor. Cocínalo hasta que la carne quede opaca.');
        INSERT PlatNotaReferencia(NotaClinicaId, Orden, Titulo, Url) VALUES
            (@nid, 1, N'Listeria en inmunocomprometidos', N'https://pmc.ncbi.nlm.nih.gov/articles/PMC11486915/');
    END

    COMMIT;
    PRINT 'seed-notas-clinicas: OK.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    PRINT 'seed-notas-clinicas: ABORTADO, se hizo ROLLBACK. Nada se cargó.';
    THROW;   -- re-lanza el error claro (qué destino faltó)
END CATCH
GO

/* ---------- Verificación (criterios de aceptación F1) ---------- */
-- 1) 23 notas, TODAS sin revisar.
SELECT
    (SELECT COUNT(*) FROM PlatNotaClinica)                               AS TotalNotas,          -- 23
    (SELECT COUNT(*) FROM PlatNotaClinica WHERE RevisadaPorMedico = 1)   AS NotasRevisadas,      -- 0  ← EL CANDADO
    (SELECT COUNT(*) FROM PlatNotaClinica WHERE TipoDestino = 'Grupo')       AS NotasGrupo,      -- 17
    (SELECT COUNT(*) FROM PlatNotaClinica WHERE TipoDestino = 'Ingrediente') AS NotasIngrediente,-- 6
    (SELECT COUNT(*) FROM PlatNotaSeccion)                               AS TotalSecciones,
    (SELECT COUNT(*) FROM PlatNotaReferencia)                           AS TotalReferencias;

-- 2) Ninguna nota sin secciones (nota vacía = trampa "ausencia disfrazada de contenido").
SELECT n.Id, n.TipoDestino, n.DestinoId, n.Titulo
FROM PlatNotaClinica n
WHERE NOT EXISTS (SELECT 1 FROM PlatNotaSeccion s WHERE s.NotaClinicaId = n.Id);  -- debe venir vacío
GO
