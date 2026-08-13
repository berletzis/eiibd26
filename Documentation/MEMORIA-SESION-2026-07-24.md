# Memoria de sesión — 24 JUL 2026

Continuación. Foco: cierre del módulo de tolerancia + UX, sidebars, ocultar acciones de paciente a médico/admin, fixes del glosario, y crawler (CCF + MAS VIDA). Doc interno, **fuera de git** (como los anteriores de sesión).

## Lecciones para NO repetir errores
- **`dotnet publish` NO caza todos los errores de Razor — CORRECCIÓN (24 JUL tarde).** El publish usa el Razor del **SDK instalado** (9.x/10-preview, regla relajada) → puede pasar limpio; pero `AddRazorRuntimeCompilation()` usa Razor **8.0.0**, más estricto → truena con el mismo `.cshtml`. Dos parsers, veredictos opuestos. "Publish limpio" **NO garantiza** que el run local (que usa runtime compilation) esté limpio. Para cazar errores de esa clase (ej. un comentario `@* *@` dentro de la zona de atributos de un `asp-for` → RZ1031): **correr la app en Development y pegarle a la página** (Razor Pages compila la vista al seleccionar el endpoint, antes de auth → un request sin login basta). El publish sí sirve como gate para C#/errores que ambos parsers ven, pero no es infalible para Razor.
- **`AddRazorRuntimeCompilation()` solo se registra en Development (`Program.cs:211`).** → Errores de compilación de vista de esa clase son **solo de desarrollo**; prod sirve vistas precompiladas (el publish output tiene 0 `.cshtml`). Si el publish rechaza el `.cshtml`, ni se despliega; si lo acepta, prod lo sirve precompilado. En ambos casos, prod no ve ese error — solo el run local (que además apunta a la BD de prod, por eso "parece en vivo").
- **No afirmar estado de la BD de prod sin consultarlo directo.** Un REQ dijo que un SQL "ya se corrió" y era falso (la columna no existía). Verificar contra prod (query a `sys.columns`) antes de asumir.
- **Editar `web.config` con editor de TEXTO PLANO**, validar el XML (arrastrarlo a un navegador marca la línea rota) y **tener copia para restaurar rápido**. Un XML roto tiró TODO el sitio (500). El sitio funcionaba con la key vieja, así que restaurar levanta a los pacientes primero.
- **Rotación de keys debe propagarse al `web.config` de prod + reiniciar.** Un 401 de Claude API era la key vieja (revocada) aún en config. El panel API Keys es read-only, sirve para verificar el prefijo.
- **Docs de seguridad de prod (que describen superficie de ataque) NO entran a git.** Feature REQs sí. Antes de commitear docs, escanear por términos sensibles + `git diff --cached`.
- **Crawler: verificar robots.txt primero (gate), confirmar que la fuente es de EII, y confirmar la URL real del sitemap desde el entorno del crawler** (mi `web_fetch` muestra el XML como vacío — no es que no exista; NINA sí lo parsea). NINA-WorkerService es fuera de scope formal, pero la config de fuentes (`fuentes.json`) es excepción aceptada.
- **Regla de identidad del validador:** nombre + foto real solo con badge `verificado`/`perfil_reclamado`; sin badge → "Profesional verificado" + avatar por defecto. NO es bug.
- **Dos copias divergentes del sidebar** (Versión A = `Areas/Identity/...` = paneles reales; Versión B = `Pages/Shared/...` = raíz). Esa divergencia fue la causa raíz de opciones de menú faltantes. Unificar en un solo partial algún día (mejora futura).
- **Patrón "paciente puro"** para ocultar acciones de perfil a médico/admin: `IsInRole("Paciente") && !Administrador && !Medico`. Excepción: cuando hay gancho para anónimo (ej. "Tu experiencia"), usar `!(Administrador || Medico)` para conservar el anónimo.
- **Regla de fondo (visibilidad):** lo comunitario y los datos los ven todos; el seguimiento/experiencia personal del paciente, solo pacientes.

## Verificar después (en vivo / navegador, por perfil)
- **Sidebars** (5 escenarios por rol): Médico ve "Mis P&R"; Admin ve "Laboratorios"; paridad de menú en ambas versiones; sin fugas entre perfiles.
- **Ocultar acciones paciente** (4 escenarios): Paciente ve "Agregar" (glosario), "Agregar a no tolerados" (/tolero) y "Tu experiencia"; Médico/Admin NO; anónimo conserva su gancho; siguen viendo comunidad + rating.
- **Glosario fixes** (4 bugs): foto del médico en "Relación con EII"; foto+comentario en "Validado por Profesionales de la Salud" (**ojo: el médico de prueba necesita el badge para verse con nombre+foto**); sin "(IA)" en ningún lado; avatar por defecto para pacientes sin foto.
- **Tolerancia** (si no se verificó ya): panel admin (query traduce + invariantes n Todos ≥ Reg.con.cond ≥ Crohn+CUCI); ficha vs /tolero mismo número; flujo no-tolerados; copiar liga; E2E profesionales.

## Pendiente de runtime (tuyo)
- **Correr el worker NINA** con la Voyage key contra prod para indexar **CCF** y **MAS VIDA**. Hasta entonces, configuradas pero sin candidatos de referencia.
- Deploy-gate SQL de features previas (si algún despliegue quedó pendiente): `add-medicodirectorio-titulo.sql`, `create-plat-tolero-envio.sql` + reinicio para sembrar rol `MedicoPendiente`.

## Decisiones pendientes
- **Flujo de personalización de platillos** (CTAs a `UsuarioAlimentacion` en Detalle/Index/Ingrediente): ¿ocultarlo también a médico/admin? Es de la misma familia que "Tu experiencia" (personal del paciente); recomendación mía = sí, pero es REQ aparte (3 páginas, cambia UX). Sin decidir.
- **#8 / #9 seguridad** (limpiar HEAD + .gitignore; purga de historia) — higiene, no riesgo activo.

## Estado
- Muchos commits pusheados a master a lo largo de la sesión (glosario, sidebars, ocultar-acciones, tolerancia completa, crawler MAS VIDA). El usuario reportó tolerancia + profesionales **ya en producción**.

## TipoProfesional (médico vs nutriólogo) — estado al cierre (24 JUL)
- **El "move" de `TipoProfesional` (ficha `MedicoDirectorio` → perfil por-usuario `MedicoPerfilExtendido`) NO se implementó.** Se eligió **Opción B: quedarse pre-move** (no escribir 11 archivos sin probar durante una caída).
- **Estado real en prod:** `MedicoDirectorio.TipoProfesional` existe (corrió `SQL/add-medicodirectorio-tipoprofesional.sql`); la columna en `MedicoPerfilExtendido` **NO** existe. El SQL `add-medicoperfilextendido-tipoprofesional.sql` queda **untracked y sin correr** — solo si algún día se retoma el move.
- **Idea pendiente (el porqué del move):** hoy `TipoProfesional` vive en la ficha, así que un profesional **sin ficha vinculada** (ej. la cuenta del fundador) NO puede elegir su tipo desde el perfil. Para que cualquiera lo elija sin reclamar ficha, hay que mover el dato a `MedicoPerfilExtendido`. REQ escrito: `REQ-mover-tipoprofesional-a-perfil-usuario.md`. Si se retoma: aplicar los 11 archivos **completos**, correr `add-medicoperfilextendido` ANTES del código, y verificar el run local en Development (no solo publish).
- **Fix aplicado hoy:** `PerfilMedico.cshtml` — comentario Razor movido fuera de la zona de atributos (RZ1031 dev-only). Commit `fix(razor): RZ1031 en PerfilMedico`.

## Verificar / pendiente al cierre
- Abrir el **Dashboard médico logueado** para cerrar el E2E (solo se verificó la capa SQL: la columna existe y el JOIN devuelve filas).
- Claude Code marcó: `appsettings.Production.json` **sigue con la contraseña `sa` viva en el repo** (el usuario gestiona seguridad a su manera; anotado sin acción).
- La diferenciación médico/nutriólogo (Opción B suave) **NO está activa** — el TOP de "Mis Validaciones" es clínico para todos hasta que se retome el move.
