# REQ — Reconciliar y arreglar el estado roto de TipoProfesional (2 errores en vivo)

**Fecha:** 24 JUL 2026
**Scope:** `eiibd26.Web`. NO tocar NINA ni Conectar3eros.
**Prioridad: sitio con errores en vivo.** Primero **estabilizar** (los 2 errores fuera), luego dejar el feature bien.
**Modo:** una sola pasada limpia, `dotnet publish -c Release` como gate (cachea las vistas Razor), diff antes de aplicar.

## Síntomas (en vivo)
1. **`PerfilMedico.cshtml`** al editar perfil: *"An error occurred during the compilation of a resource… TagHelper attributes must be well-formed."* (error de sintaxis Razor, clase RZ).
2. **Dashboard médico** (`/Identity/Medico/Dashboard`): *`SqlException: Invalid column name 'TipoProfesional'`*.
El usuario hizo Clean + Rebuild en VS y **sigue igual**.

## Diagnóstico del advisor (verificado leyendo el código local)
El "move" de `TipoProfesional` (ficha → perfil por-usuario) **NO está aplicado en el código local**:
- `Models/Directorio/MedicoDirectorio.cs:38` **todavía tiene** `public TipoProfesional? TipoProfesional`.
- `Models/Medico/MedicoPerfilExtendido.cs` **NO tiene** `TipoProfesional`.
- `Areas/Identity/Pages/Medico/Dashboard.cshtml.cs:62` **lee** `perfil.Medico?.TipoProfesional` (de la ficha).
- `Areas/Identity/Pages/Account/Manage/PerfilMedico.cshtml:287` **tiene** el `disabled="@(!Model.PerfilVinculado)"` viejo.

→ El working tree = estado **pre-move** (el que funcionaba esta mañana). Que los errores persistan tras un rebuild local sugiere **desincronía**: lo que se ejecuta/despliega no es este código, o hay un estado a medias en git/deploy. **Reconciliar esto es el paso 0.**

## Tarea

### Paso 0 — Reconciliar el estado real (antes de tocar nada)
- `git status`, `git log --oneline -15`, rama actual. ¿Existe un commit del move? ¿El working tree está limpio o hay cambios sin commitear? ¿Lo que corre es este build o uno desplegado distinto?
- Determinar cuál de los dos mundos es el bueno y **alinear código + BD + lo que corre**. Reportar antes de aplicar.

### Paso 1 — Arreglar error A (PerfilMedico, Razor)
- Correr `dotnet build` / `dotnet publish -c Release`: **nombra la línea exacta** del atributo mal formado. Arreglarlo.
- Candidato a revisar: el `<select asp-for="Input.TipoProfesional">` (PerfilMedico.cshtml ~285-294) y que `Model.PerfilVinculado` exista como propiedad del PageModel (si no existe o cambió de nombre, ese es el problema). Regla de siempre: booleano **bindeado al atributo**, nunca C# suelto (RZ1031).

### Paso 2 — Arreglar error B (Dashboard, columna)
- Reconciliar **código ↔ BD**:
  - Si se queda **pre-move**: el código lee `MedicosDirectorio.TipoProfesional` → garantizar que esa columna existe en la BD que usa la app (re-correr `SQL/add-medicodirectorio-tipoprofesional.sql`, idempotente).
  - Si se aplica el **move**: el código lee `MedicoPerfilExtendido.TipoProfesional` → garantizar esa columna (`SQL/add-medicoperfilextendido-tipoprofesional.sql`, ya corrido). Y que el Dashboard/consultas ya NO referencien `MedicoDirectorio.TipoProfesional`.

### Paso 3 — Elegir UN camino limpio (no dejar medio estado)
Opción a decidir por Claude Code y reportar:
- **(A) Completar el move bien:** aplicar los 11 archivos del diff completo, `publish` limpio, correr el SQL de `MedicoPerfilExtendido` **antes** de que el código quede vivo, desplegar, y el `drop` de la columna vieja **después**.
- **(B) Revertir el move:** dejar todo pre-move (working de la mañana) + asegurar `MedicosDirectorio.TipoProfesional`, `publish` limpio. Restaura servicio y el move se rehace otro día.
Cualquiera sirve — pero **una sola pasada, verificada**, no otro estado a medias.

## Verificación (obligatoria antes de declarar hecho)
1. `dotnet publish -c Release` **limpio** (0 errores, 0 RZ) — esto caza el error A antes de desplegar.
2. La página **Editar perfil** (`PerfilMedico`) carga sin el error de Razor.
3. El **Dashboard médico** carga sin `Invalid column name`.
4. Si se completó el move: poner "Nutriólogo" desde el perfil (sin ficha) guarda y persiste; "Mis Validaciones" cambia el TOP a alimentos.
5. Orden de despliegue respetado (SQL de columnas ANTES del código; drop de la vieja después).
