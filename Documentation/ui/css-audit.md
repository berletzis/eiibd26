# EIIBD — Auditoría CSS / Design System
**Fecha:** 2026-05-26  
**Fase:** 1 — Inventario y diagnóstico  
**Objetivo:** Línea base para la migración al nuevo Design System `eii-*`

---

## 1. Inventario de archivos CSS propios

| Archivo | KB | Líneas | Selectores | `:root` | CSS vars | `!important` | Colores HC |
|---|---|---|---|---|---|---|---|
| `detalle.css` | 32.6 | 1,465 | 189 | 1 | 39 | 19 | 121 |
| `preguntas.css` | 31.3 | 1,518 | 143 | 1 | 14 | 22 | 136 |
| `site.css` | 15.8 | 624 | 57 | 1 | 14 | 40 | 75 |
| `miSalud.css` | 12.6 | 575 | 55 | 0 | 0 | 30 | 74 |
| `directorio-medicos.css` | 11.2 | 503 | 66 | 1 | 19 | 1 | 60 |
| `contenidos-cards.css` | 10.8 | 486 | 52 | 0 | 0 | 0 | 8 |
| `usuario-condiciones-crm.css` | 9.6 | 377 | 61 | 1 | 7 | 21 | 60 |
| `site-responsive.css` | 9.1 | 404 | 18 | 1 | 7 | 11 | 30 |
| `account.css` | 7.1 | 313 | 23 | 1 | 9 | 7 | 43 |
| `accessibility-fixes.css` | 2.8 | 113 | 20 | 1 | 2 | 0 | 41 |
| `perfil.css` | 1.5 | 74 | 8 | 0 | 0 | 0 | 13 |
| `avatar-card.css` | 1.3 | 69 | 8 | 0 | 0 | 0 | 7 |
| **TOTAL** | **145.7** | **5,524** | **700** | **8** | **111** | **151** | **668** |

HC = Hardcoded (hex / rgb / rgba fuera de variables)

---

## 2. Orden de carga CSS en `_Layout.cshtml`

```
1. account.css          ← ANTES de Bootstrap (error de cascada)
2. site.css             ← ANTES de Bootstrap
3. miSalud.css          ← ANTES de Bootstrap
4. usuario-condiciones-crm.css  ← Global, TODOS los usuarios, ANTES de Bootstrap
5. site-responsive.css  ← ANTES de Bootstrap
6. Bootstrap 5.3.2 CDN  ← Bootstrap puede sobreescribir pasos 1-5
7. accessibility-fixes.css  ← Overrides Bootstrap (correcto)
8. Bootstrap Icons CDN
9. DataTables CSS
10. @section Styles      ← Por página, siempre gana la cascada
```

### Problemas críticos de carga
- **`bundle.min.css` referenciado pero NO existe** — referencia muerta en `_Layout.cshtml`
- **`usuario-condiciones-crm.css` se carga dos veces**: global en `_Layout` + `@section Styles` en UsuarioCondiciones, UsuarioTratamientos, UsuarioSintomas, UsuarioLaboratorios
- **CSS propios cargan ANTES de Bootstrap**: Bootstrap puede sobreescribir selectores no-namespaced. Esto explica la proliferación de `!important` (151 instancias)

---

## 3. Fragmentación del sistema de tokens

**8 bloques `:root` distintos**, cada uno define su propia versión del color primario:

| Archivo | Variable | Valor |
|---|---|---|
| `site.css` | `--brand-color` | `#546e76` (teal gris) |
| `usuario-condiciones-crm.css` | `--crm-accent` | `#7c3aed` |
| `directorio-medicos.css` | `--color-primary` | `#6a4e7a` (recién corregido) |
| `preguntas.css` | `--se-answered` | `#6a4e7a` |
| `detalle.css` | `--color-primary` | `var(--brand-color)` → `#546e76` |
| `account.css` | variables propias | múltiples |
| `accessibility-fixes.css` | `--bs-secondary-color` | `#546a70` |
| `site-responsive.css` | `--brand-color` | definición propia |

**Resultado**: no existe un `--color-primary` único y compartido. Cada módulo tiene su propia idea del color de marca.

---

## 4. Inventario de sistemas de botones

Se encontraron **~35 clases de botones distintas** en producción:

### Bootstrap base (reutilizado)
`btn` · `btn-primary` · `btn-outline-primary` · `btn-outline-secondary` · `btn-secondary` · `btn-success` · `btn-outline-success` · `btn-outline-danger` · `btn-sm` · `btn-close` · `btn-check`

### Sistema CRM (condiciones/síntomas/tratamientos)
`crm-btn` · `crm-btn-primary` · `crm-btn-secondary` · `crm-btn-success` · `crm-btn-cancel` · `crm-btn-delete`

### Sistema SE (preguntas/comunidad)
`se-btn` · `se-btn-primary` · `se-btn-ghost` · `se-btn-link`

### Directorio médicos
`directorio-btn-buscar`

### Admin/gestión
`btn-action-grid` · `btn-action-edit` · `btn-action-delete`

### Auth/acceso
`btn-auth-notice-login` · `btn-auth-notice-register` · `btn-modal-login` · `btn-modal-register`

### Ad-hoc / únicos
`btn-brand` (reciente) · `btn-login-hint` · `btn-login-hint-primary` · `accion-btn-primary` · `circle-btn` · `btn-add-mood` · `btn-agregar` · `btn-track-sintoma` · `responder-boton` · `responder-boton-link` · `sidebar-toggle-inline` · `vote-auth-modal__close` · `se-vote-btn` · `se-delete-btn` · `se-delete-q` · `se-share-invite__btn`

**Total sistemas de botones:** 4 (CRM + SE + Admin + Bootstrap) + docenas ad-hoc

---

## 5. Inventario de sistemas de cards

| Sistema | Clases | Archivo | Módulos |
|---|---|---|---|
| CRM | `crm-card`, `crm-card-condicion`, `crm-card-inner`, `crm-card-header`, `crm-card-badges`, `crm-card-section` | `usuario-condiciones-crm.css` | Condiciones, Síntomas, Tratamientos, Laboratorios |
| SE Content | `se-card`, `se-card-body`, `se-card-meta`, `se-card-title`, `se-card-excerpt`, `se-card-tags` | `contenidos-cards.css` | Contenidos, Glosario, Home |
| Site | `card-block`, `card-block-modern` | `site.css` | Admin, perfiles |
| Directorio | `medico-card`, `medico-card__header`, `medico-card__info`, `medico-card__footer` | `directorio-medicos.css` | Directorio médicos |
| Sidebar | `sidebar-section`, `sidebar-static` | `directorio-medicos.css`, `detalle.css` | Directorio, Contenidos |
| Bootstrap | `.card` | Bootstrap | Disperso |

**Total sistemas de cards:** 6 distintos

---

## 6. Inventario de inputs

| Clase | Archivo | Módulos donde aparece |
|---|---|---|
| `form-control` | Bootstrap global | Todo el sistema |
| `crm-input`, `crm-input-date` | `usuario-condiciones-crm.css` | Condiciones, Síntomas, Tratamientos, Labs, Directorio |
| `se-search-input` | `preguntas.css`, `contenidos-cards.css` | Preguntas, Contenidos, Directorio |
| `crm-search-horizontal` | `usuario-condiciones-crm.css` | Búsquedas CRM |

Sin sistema de inputs unificado. Focus/hover/error/disabled varían por módulo.

---

## 7. Inline styles — 677 instancias en 74 archivos

### Top 10 archivos con más inline styles
| Archivo | Instancias |
|---|---|
| `Admin/Usuarios/Index.cshtml` | 78 |
| `Shared/_SidebarMenu.cshtml` | 51 |
| `Shared/_SidebarMenu.cshtml` (Identity) | 47 |
| `Glosario/Index.cshtml` | 30 |
| `Glosario/Termino.cshtml` | 42 |
| `UusuarioPreguntaDetalle.cshtml` | 20 |
| `Pages/u/Index.cshtml` | 12 |
| `Mapa/Index.cshtml` | 12 |
| `UsuarioSintomas.cshtml` | 17 |
| `UsuarioTratamientos.cshtml` | 18 |

### Casos JS que inyectan inline styles (requieren refactor de JS)
- `_TrackingSintomaModal.cshtml` — JS genera botones con inline styles
- `_EstadoAnimoModal.cshtml` — mood buttons via JS
- `UsuarioSintomasSeguimiento.cshtml` — tabla tracking con JS
- Gráficas Chart.js — colores vía JS config

---

## 8. Mapa módulo → CSS actual

| Módulo | CSS cargado | Total archivos |
|---|---|---|
| Home / Inicio | `site.css` + `miSalud.css` + `contenidos-cards.css` + `preguntas.css` | 4 |
| Preguntas (lista) | `preguntas.css` + `contenidos-cards.css` | 2 |
| Preguntas (detalle) | `preguntas.css` + `contenidos-cards.css` + `detalle.css` | 3 |
| Contenidos / Index | `contenidos-cards.css` | 1 |
| Contenidos / Detalle | `detalle.css` | 1 |
| Contenidos / porCategoría | `contenidos-cards.css` | 1 |
| Glosario / Index, Síntomas, Tratamientos | `contenidos-cards.css` | 1 |
| Glosario / Termino | `detalle.css` | 1 |
| DirectorioMedicos / * | `directorio-medicos.css` | 1 |
| Dashboard Paciente | `miSalud.css` + `site.css` + `usuario-condiciones-crm.css` | 3 |
| Condiciones / Síntomas / Tratamientos / Labs | `usuario-condiciones-crm.css` × 2 (doble carga) | 2 |
| PerfilMedico / UsuarioPerfil | `avatar-card.css` + `perfil.css` | 2 |
| Medico / Dashboard | `directorio-medicos.css` | 1 |
| Admin / * | `site.css` + DataTables | 2 |
| Login / Register | `account.css` | 1 |

---

## 9. Dependencia entre archivos CSS

```
_Layout.cshtml
  ├── account.css
  ├── site.css
  ├── miSalud.css
  ├── usuario-condiciones-crm.css   ← global (todos los usuarios)
  ├── site-responsive.css
  ├── Bootstrap CDN
  └── accessibility-fixes.css

contenidos-cards.css
  └── @import detalle.css           ← dependencia implícita

detalle.css
  └── usa var(--brand-color)        ← depende de site.css para resolverse
```

---

## 10. Problemas estructurales identificados

### P1 — CRÍTICO: No existe un Design Token único
8 bloques `:root` distintos. `--color-primary` tiene 3 valores diferentes según qué CSS cargó último.

### P2 — CRÍTICO: Orden de carga CSS incorrecto
CSS propio carga antes de Bootstrap. Los overrides de Bootstrap requieren `!important` (151 instancias).

### P3 — ALTO: `bundle.min.css` referenciado pero inexistente
`_Layout.cshtml` líneas 73, 80, 97 referencian `bundle.min.css`. No existe. El código tiene un fallback pero es deuda técnica.

### P4 — ALTO: `usuario-condiciones-crm.css` cargado globalmente
9.6 KB descargados en cada página, incluso en páginas públicas que no usan nada de ese CSS.

### P5 — ALTO: 4 sistemas de botones incompatibles
CRM, SE, Admin y Bootstrap conviven. Cada módulo tiene su propio `btn-*`.

### P6 — ALTO: 6 sistemas de cards incompatibles
Sin estándar visual común entre módulos.

### P7 — MEDIO: `contenidos-cards.css` importa `detalle.css`
Dependencia implícita. Si se elimina `detalle.css` se rompe `contenidos-cards.css`.

### P8 — MEDIO: Google Fonts Inter cargado 3 veces per-page
`UsuarioCondiciones`, `UsuarioTratamientos`, `UsuarioSintomas` cada uno carga Inter independientemente. No está en el layout global.

### P9 — BAJO: 35+ clases de botones ad-hoc sin documentar
Muchas creadas puntualmente y nunca consolidadas al sistema.

---

## 11. Resumen numérico

| Métrica | Valor |
|---|---|
| Archivos CSS propios | 12 |
| Tamaño total | 145.7 KB |
| Líneas totales | 5,524 |
| Selectores totales | 700 |
| Bloques `:root` (tokens) | 8 |
| Variables CSS definidas | 111 |
| `!important` | 151 |
| Colores hardcodeados | 668 |
| Inline styles en vistas | 677 |
| Vistas con inline styles | 74 |
| Sistemas de botones | 4 + 35 ad-hoc |
| Sistemas de cards | 6 |
| Sistemas de inputs | 3 |

---

## 12. Archivos destino del nuevo Design System

```
wwwroot/css/
  eiibd-tokens.css        ← Fase 3: variables únicas
  eiibd-layout.css        ← Fase 8: page, section, grid, stack
  eiibd-components.css    ← Fases 5-7: eii-card, eii-btn, eii-input
  eiibd-utilities.css     ← Fase 9: espaciado, flex, visibilidad
  eiibd-overrides.css     ← Overrides Bootstrap (extrae de accessibility-fixes.css)
```

### Archivos a eliminar (cuando sus módulos migren al 100%)
`detalle.css` · `preguntas.css` · `contenidos-cards.css` · `directorio-medicos.css` · `usuario-condiciones-crm.css` · `miSalud.css` · `site-responsive.css` · `account.css` · `perfil.css` · `avatar-card.css`

### Archivos a conservar / absorber
`site.css` → absorber en `eiibd-tokens.css` + `eiibd-layout.css`  
`accessibility-fixes.css` → absorber en `eiibd-overrides.css`

---

## 13. Orden de migración por módulos (Fase 10)

### Bloque 1 — Público (estabiliza el Design System)
1. Login / Register (`account.css`)
2. Home (`contenidos-cards.css`, `preguntas.css`)
3. Contenidos/Index y Detalle (`contenidos-cards.css`, `detalle.css`)
4. Preguntas (lista + detalle)
5. Glosario (Index + Termino + Síntomas + Tratamientos)
6. Ayuda / General
7. Mapa comunidad
8. Directorio médicos (Index + Detalle + Proponer + Reclamar)

### Bloque 2 — Panel Paciente (reutiliza Design System estabilizado)
9. Dashboard Paciente
10. Estado Ánimo
11. Condiciones
12. Síntomas
13. Seguimiento síntomas
14. Tratamientos
15. Laboratorios
16. Perfil paciente

### Bloque 3 — Áreas autenticadas
17. Médico Dashboard + Perfil
18. Admin (tablas DataTables, gestión)

---

*Generado automáticamente — 2026-05-26*
