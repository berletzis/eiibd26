# TAREA (backlog) — Separar el sidebar en admin vs paciente

**Fecha anotada:** 17 JUL 2026
**Origen:** revisión del menú izquierdo. Hay DOS `_SidebarMenu.cshtml` duplicados que drifteron en ambas direcciones; el bug visible fue que "Lo que no tolero" faltaba en las páginas de Platillos. Ya se aplicó el band-aid (agregar ese item al sidebar root).

## El problema de fondo
Dos partials mantenidos a mano, que se desincronizan cada vez que se agrega un item:
- `Areas/Identity/Pages/Shared/_SidebarMenu.cshtml` (430 líneas) — usado por páginas del área Identity.
- `Pages/Shared/_SidebarMenu.cshtml` (326 líneas) — usado por páginas públicas root (Platillos).

## La tarea
Separar por **audiencia**, no por área, en partials distintos:
- **Sidebar de paciente** — solo lo del paciente (Perfil, Panel, P&R, Médicos, MI SALUD: ánimo, condiciones, síntomas, seguimiento, tratamientos, laboratorios, **Lo que no tolero**).
- **Sidebar de admin** — solo lo admin (Contenidos + Motor de Cobertura + Platillos admin + Usuarios + ApiKeys, etc.).
- (Opcional) sidebar de médico, si aplica.

Cada layout/página incluye el partial que corresponde a quién está viendo. Un paciente **nunca** recibe markup admin.

## Beneficio
1. **Mata el drift**: la parte de paciente deja de estar duplicada en dos archivos.
2. **Seguridad (robustez)**: se elimina la clase de bug donde un `@if(IsInRole)` mal puesto filtra nav admin — el partial de paciente no tiene HTML admin que filtrar.
3. Menos HTML por request para el paciente.

## OJO al ejecutar — resolver la UNIÓN del drift actual
Como cada sidebar tiene items únicos, NO es copiar uno sobre otro (se perderían cosas). Consolidar conservando:
- **Solo en el de Identity (falta en el root):** UsuarioAlimentacion (ya band-aid), y toda la sección admin actual — Contenidos/Calidad, Cobertura, Contenidos, Embeddings, Firmas, FirmasExternas, Oportunidades, Similitud, SimilitudEmbedding; Platillos admin: Atributos, Categorias, Grupos, Ingredientes, Unidades; ApiKeys.
- **Solo en el root (falta en el de Identity):** Laboratorios/Index, Laboratorios/Resultados, MedicoPreguntasRespuestas, y `Contenidos/Index` (versión vieja — decidir si se reemplaza por los granulares).

## Notas
- Es contenido/organización, no arquitectura pesada. Riesgo bajo.
- Probar en LOS DOS contextos (root Pages e Identity area): que el helper `IsActive` funcione igual y que los `@if` de rol resuelvan bien.
- `.cshtml` → sin rebuild en VS, con RazorRuntimeCompilation basta refrescar.
