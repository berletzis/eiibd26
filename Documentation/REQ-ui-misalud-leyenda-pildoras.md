# REQ — Mi Salud: espaciado de la leyenda + estandarizar píldoras removibles

**Fecha:** 17 JUL 2026
**Alcance:** páginas de Mi Salud (Condiciones, Síntomas, Tratamientos, Estado de ánimo, Laboratorios, Seguimiento). CSS + `.cshtml`, **sin rebuild**. Reusar tokens `eii-*`. Diff antes de aplicar. Verificar en vivo (con el candado `EIIBD_DISABLE_BACKGROUND_WORKERS=1`).

## Fix 1 — Leyenda "Lista de..." pegada arriba/abajo
La leyenda ("Lista de todos los Tratamientos", "Lista de resultados registrados", etc.) usa `.condiciones-paginacion`, compartida por las **6 páginas**. Pero esa clase **no tiene ninguna regla CSS** hoy (se perdió en el borrado de `miSalud.css`, commit `e7d4dae` — misma raíz que el card buscador). Por eso va apretada.

**Arreglo:** agregar una regla para `.condiciones-paginacion` con ritmo vertical, ej.:
```css
.condiciones-paginacion {
    margin: var(--eii-space-5) 0 var(--eii-space-3);
    font-weight: var(--eii-fw-semibold);
    color: var(--eii-text-soft);
}
```
(Ajustar los tokens de espacio al que se vea bien.) Un solo punto arregla las 6.

## Fix 2 — Estandarizar las píldoras removibles a gris + tache rojo
Las píldoras de relación (síntoma↔condición↔tratamiento, con la ⓧ roja para quitar) hoy están **inconsistentes por posición, no por entidad**:
- Gris (`bg-info`): condición en Síntomas, síntoma en Tratamientos.
- Morado relleno (`bg-success text-white`): condición en Tratamientos, tratamiento en Síntomas.

El morado relleno choca con el tache rojo (lo que reportó el owner). **Estándar deseado: el gris (`bg-info`) con la ⓧ roja.**

**Arreglo:** cambiar las píldoras `bg-success text-white` → `bg-info` (quitar `text-white`; `bg-info` es claro y necesita texto oscuro). Puntos concretos:
- `UsuarioTratamientos.cshtml:177` — tag de **condición** (el reportado, "PANCOLITIS" morado).
- `UsuarioSintomas.cshtml:141` — tag de **tratamiento** (mismo morado, mismo choque; corregir de paso para que el estándar sea real y consistente).

Así **todas** las píldoras removibles quedan gris + tache rojo, en todas las páginas.

> Opción más limpia (opcional): en vez de depender de `bg-info`/`bg-success` (clases de Bootstrap recoloreadas), definir una clase dedicada `.eii-tag--removable` (fondo gris claro `--eii-surface-subtle`, texto `--eii-text`, la ⓧ en `--eii-danger`) y usarla en las 4 ubicaciones. Es el estándar "de verdad" y desacopla de Bootstrap. Si se hace, aplica a los tags de relación en Síntomas y Tratamientos.

## Verificación
- La leyenda "Lista de..." respira (margen arriba/abajo) en las 6 páginas.
- Ninguna píldora removible queda morada rellena; todas gris + ⓧ roja, en Síntomas y Tratamientos.
- Sin rebuild; diff antes de aplicar.
