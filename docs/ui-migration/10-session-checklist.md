# CHECKLIST DE SESIÓN — DESIGN SYSTEM EIIBD

> Ejecutar **antes de cerrar cualquier sesión** que haya tocado CSS, HTML o estilos.
> No dar sesión por terminada si quedan ítems sin marcar.

---

## Verificación de código nuevo

- [ ] No se crearon hardcodes de color (`#xxx`, `rgb(...)`)
- [ ] No se crearon hardcodes de espaciado fuera de la escala de 4px
- [ ] No se usó `style="..."` inline en HTML (salvo excepciones documentadas)
- [ ] No se agregó `!important` sin justificación escrita
- [ ] Todo nuevo componente usa prefijo `eii-`
- [ ] Se usaron tokens (`var(--eii-*)`) en lugar de valores directos
- [ ] Se usó la escala de espaciado estándar (space-1 a space-10)
- [ ] No se duplicó un componente que ya existe en el sistema

## Reutilización

- [ ] Se revisaron componentes existentes antes de crear nuevos
- [ ] Se extendió con modificador BEM en vez de crear variante nueva
- [ ] No se copió CSS de páginas viejas como base

## Legacy / migración

- [ ] No se crearon archivos CSS nuevos fuera del sistema `eiibd-*.css`
- [ ] Los bloques legacy tocados están marcados con comentario `LEGACY`
- [ ] No se eliminó CSS legacy de páginas que aún no fueron migradas

## Documentación

- [ ] Las excepciones nuevas están registradas en `09-project-rules.md` sección 16
- [ ] Si se modificó `style-guide.html`, refleja los cambios reales del sistema
- [ ] Memoria del proyecto actualizada si hubo decisiones nuevas

## Build

- [ ] `dotnet build --no-restore` → **0 errores**

---

## Checklist rápido de grep (ejecutar en terminal)

```powershell
# Buscar residuos en archivos .cshtml y .css
# Hardcodes de color
grep -rn "#7c3aed\|#6d28d9\|#764ba2\|#667eea" eiibd26/Pages eiibd26/Areas eiibd26/wwwroot/css

# Tokens legacy
grep -rn "var(--color-\|var(--space-\|var(--font-size-\|var(--se-\|var(--font-primary" eiibd26/Pages eiibd26/Areas eiibd26/wwwroot/css

# Inline styles sospechosos (revisar manualmente)
grep -rn 'style="' eiibd26/Pages eiibd26/Areas | grep -v "data-\|aria-\|content="
```

---

*Actualizado: 2026-05-26*
