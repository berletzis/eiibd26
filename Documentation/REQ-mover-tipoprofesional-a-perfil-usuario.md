# REQ — Mover `TipoProfesional` de la ficha del directorio al perfil por-usuario

**Fecha:** 24 JUL 2026
**Scope:** solo `eiibd26.Web`. NO tocar NINA ni Conectar3eros.
**Modo:** analizar primero, **diff antes de aplicar**, build `dotnet publish` (Razor) antes de pushear.
**Problema:** `TipoProfesional` se guardó en `MedicoDirectorio` (la ficha pública del directorio), así que un profesional **sin ficha vinculada** (ej. la cuenta del fundador, y cualquier médico pre-existente) **no puede elegir su tipo** — el dropdown aparece pero no guarda. Pero el tipo NO es un dato público del directorio: es una **preferencia personal de ruteo** (qué se sugiere validar primero en "Mis Validaciones"). Debe vivir en el perfil por-usuario, disponible siempre, con o sin ficha.

## Cambio de fondo
Mover `TipoProfesional` de **`MedicoDirectorio`** → **`MedicoPerfilExtendido`** (tabla real `MedicoPerfilExtendido`, singular), que TODA cuenta profesional tiene por su `UserId` (con o sin ficha). Enum sin cambios (`Models/Directorio/Enums/TipoProfesional.cs`): 1=MedicoEspecialistaEII, 2=Nutriologo, 3=Otro; **nullable** = general.

## 1. Modelo + SQL
- Agregar `public byte? TipoProfesional { get; set; }` (o el enum nullable) a `Models/Medico/MedicoPerfilExtendido.cs`.
- **Quitar** `TipoProfesional` del modelo `Models/Directorio/MedicoDirectorio.cs`.
- SQL nuevo `SQL/add-medicoperfilextendido-tipoprofesional.sql` (idempotente): `TINYINT NULL` + CHECK de dominio (1/2/3) en la tabla `MedicoPerfilExtendido`.
- SQL de migración de datos (por si alguien ya tenía tipo en la ficha): `UPDATE pe SET pe.TipoProfesional = d.TipoProfesional FROM MedicoPerfilExtendido pe JOIN MedicosDirectorio d ON d.Id = pe.MedicoId WHERE d.TipoProfesional IS NOT NULL;` (probablemente no-op, recién se desplegó).
- SQL de limpieza (drop de la columna vieja) `SQL/drop-medicodirectorio-tipoprofesional.sql` — **se corre DESPUÉS** de que el código deje de referenciarla.

## 2. Dónde se lee/escribe (mover todo a MedicoPerfilExtendido por UserId)
- **`MisValidaciones.cshtml.cs`:** leer `TipoProfesional` de `MedicoPerfilExtendido` por `UserId` (ya tiene el userId), NO de la ficha. Falla suave a null igual.
- **`PerfilMedico`:** el dropdown "Tipo de profesional" ahora **se guarda SIEMPRE** (escribe en `MedicoPerfilExtendido` del usuario), **sin** la condición "solo si hay ficha vinculada". Quitar/ajustar el texto de ayuda que sugiera que depende de la ficha — el tipo es editable aunque Nombre/Título/Especialidad sigan bloqueados por falta de ficha.
- **`RegisterM`:** guardar `Input.TipoProfesional` en el `MedicoPerfilExtendido` que ya crea (no en la ficha).
- **`Admin/DirectorioMedicos`:** al fijar el tipo, escribir en el `MedicoPerfilExtendido` del profesional (por su UserId), no en la ficha.

## 3. Orden de despliegue (CRÍTICO — misma lección de antes)
1. **Correr `SQL/add-medicoperfilextendido-tipoprofesional.sql` en prod PRIMERO** (el modelo EF nuevo declara la columna; sin ella, las lecturas de `MedicoPerfilExtendido` truenan → sitio caído). Y NO levantar la app local hasta correrlo.
2. Correr la migración de datos (copia ficha→perfil).
3. Desplegar el código (ya lee/escribe en `MedicoPerfilExtendido`, ya no referencia `MedicoDirectorio.TipoProfesional`).
4. **Después** del deploy (código ya no usa la columna vieja): correr `SQL/drop-medicodirectorio-tipoprofesional.sql`. Dejar la columna vieja mientras tanto es inofensivo (columna extra que EF ignora), así que no hay prisa.

## Fuera de alcance
- No cambiar el enum ni la lógica de ramificación del TOP (solo cambia DÓNDE vive el dato).
- No cambiar permisos (el tipo sigue sin ser un candado).
- Nombre/Título/Especialidad siguen en la ficha (esos SÍ son del directorio público) — solo se mueve Tipo.

## Verificación
1. Con la cuenta del fundador (**sin ficha vinculada**): el dropdown "Tipo de profesional" en el perfil **se puede elegir y guardar**, y persiste al recargar. Ya no depende de reclamar ficha.
2. "Mis Validaciones" refleja el tipo elegido (nutriólogo → TOP de alimentos; médico → TOP clínico), sin necesidad de ficha.
3. Un profesional con ficha vinculada también lo puede editar (mismo comportamiento).
4. El admin puede fijar el tipo de un profesional por su cuenta.
5. Orden de despliegue respetado: `add` antes del código; `drop` de la columna vieja después. `dotnet publish -c Release` limpio.
