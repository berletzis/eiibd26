# REQ — Move de TipoProfesional a perfil por-usuario (v2, endurecido con lecciones del 24 JUL)

**Fecha:** 24 JUL 2026
**Scope:** solo `eiibd26.Web`. NO tocar NINA ni Conectar3eros.
**Reemplaza/consolida:** `REQ-mover-tipoprofesional-a-perfil-usuario.md` + las correcciones de la crisis de hoy.
**Objetivo:** mover `TipoProfesional` de la ficha (`MedicoDirectorio`) al perfil por-usuario (`MedicoPerfilExtendido`), para que **cualquier profesional pueda elegir su tipo desde el perfil, con o sin ficha vinculada** (hoy solo funciona para quien se registra por el formulario; una cuenta pre-existente sin ficha —como la del fundador— no puede). Pago inmediato: la cuenta de pruebas podrá alternar nutriólogo↔médico desde su perfil.

## El diff (Claude Code YA lo mapeó — 11 archivos)
Usar el mapeo que Claude Code produjo en su análisis previo. Resumen de los movimientos:
- `MedicoPerfilExtendido.cs` — **agregar** `public TipoProfesional? TipoProfesional`.
- `MedicoDirectorio.cs:38` — **quitar** la propiedad.
- `MisValidaciones.cshtml.cs` (~121-124) — query a `MedicosPerfilExtendido` por `UserId` (no a la ficha).
- `Dashboard.cshtml.cs:62` — `perfil.TipoProfesional` (no `perfil.Medico?.TipoProfesional`).
- `PerfilMedico.cshtml.cs:232` — `Input.TipoProfesional = perfil.TipoProfesional`.
- `PerfilMedico.cshtml.cs:~400` — la escritura **sale del `if (perfil.Medico is not null)`** → guarda SIEMPRE en `perfil.TipoProfesional`.
- `PerfilMedico.cshtml:287` — **quitar** `disabled="@(!Model.PerfilVinculado)"` del `<select>`.
- `RegisterM.cshtml.cs:158` — guardar el tipo en el `MedicoPerfilExtendido` que ya crea (no en la ficha).
- `Admin/DirectorioMedicos/Index.cshtml.cs:254/330` — leer/escribir el tipo en el perfil ext. por `UserId`/`MedicoId`.

## Edge case (Claude Code ya lo resolvió — mantener)
Admin editando una ficha **sin cuenta vinculada** (propuestas de pacientes, `AspNetUserId` null): si hay perfil ext. → escribe; si hay cuenta pero no perfil → crea la fila; si no hay cuenta → no guarda, `ok:true` + aviso en el JS. Sin filas huérfanas.

## DISCIPLINA DE PROCESO — lecciones de hoy (obligatorias)
1. **Aplicar el diff COMPLETO en una sola pasada y COMMITEARLO.** Hoy el move nunca quedó aplicado (working tree pre-move) → confusión de horas. Al terminar: `git status` limpio, el commit del move presente. No dejar medio estado.
2. **SQL — el archivo YA existe:** `SQL/add-medicoperfilextendido-tipoprofesional.sql` (creado en la crisis; idempotente: columna TINYINT NULL + CHECK 1/2/3 + backfill ficha→perfil envuelto en `IF EXISTS`). Verificar que esté en el repo.
3. **Orden de despliegue (esto nos tumbó el sitio DOS veces hoy):**
   - **a.** Correr `add-medicoperfilextendido-tipoprofesional.sql` en prod **ANTES** de que el código quede vivo. **Verificar contra prod que la columna existe** (`SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MedicoPerfilExtendido') AND name = 'TipoProfesional'`) — NO asumir que corrió.
   - **b.** Desplegar el código.
   - **c.** **Después:** `SQL/drop-medicodirectorio-tipoprofesional.sql` (dropea la columna vieja, ya no referenciada). La vieja mientras tanto es inofensiva.
   - No levantar la app local (que apunta a prod) hasta que la columna exista.
4. **Verificar la VISTA, no solo `publish`.** Hoy un comentario `@* *@` dentro de la zona de atributos de un `asp-for` pasó el `publish` (Razor del SDK, relajado) pero tronó en runtime compilation (Razor 8.0.0, estricto). Entonces: `dotnet publish -c Release` limpio **Y ADEMÁS** correr la app en **Development** (con `EIIBD_DISABLE_BACKGROUND_WORKERS=1`) y **abrir físicamente** PerfilMedico, Dashboard médico y MisValidaciones para confirmar que no truena la compilación de vista. Un request sin login basta (la vista compila al seleccionar el endpoint).

## Verificación
1. Cuenta **sin ficha** (la del fundador): el dropdown "Tipo de profesional" en el perfil **se puede elegir y GUARDAR**, persiste al recargar. Ya no depende de reclamar ficha.
2. Poner "Nutriólogo" → "Mis Validaciones" muestra el TOP de **ingredientes/grupos**. Cambiar a "Médico especialista EII" → TOP **clínico**. Alternar funciona desde el perfil, sin SQL.
3. Un profesional **con** ficha también edita su tipo igual.
4. Admin puede fijar el tipo de un profesional por su cuenta (con el edge case cubierto).
5. `git status` limpio + commit del move presente.
6. `publish` limpio **Y** las 3 vistas abren sin error en Development.
7. Orden de despliegue respetado: add-SQL + verificación contra prod ANTES del código; drop de la vieja después.
