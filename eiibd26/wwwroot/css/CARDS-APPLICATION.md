# 🎨 Aplicación del Sistema Tipográfico a Páginas de Cards

## ✅ Completado

### 1. **Archivo CSS Creado**
- `wwwroot/css/contenidos-cards.css`
- Contiene todas las variables del sistema tipográfico
- Estilos base para cards, grids, filtros, paginación

### 2. **Variables CSS Aplicadas**
Las siguientes variables están disponibles y deben usarse:

```css
/* Tipografía */
--font-primary
--font-size-xs hasta --font-size-4xl
--line-height-base
--line-height-heading

/* Colores Morados */
--color-primary: #7c3aed
--color-primary-hover: #6d28d9
--color-primary-bg: #f3e8ff

/* Colores de Texto */
--color-text-primary: #1f2937
--color-text-secondary: #6b7280
--color-heading: #111827

/* Espaciado */
--space-xs hasta --space-3xl

/* Otros */
--color-bg: #ffffff
--color-border: #e5e7eb
```

---

## 📝 Páginas Actualizadas

### ✅ Contenidos/Index.cshtml
- **Cambio**: Agregado `<link rel="stylesheet" href="~/css/contenidos-cards.css" />`
- **Estilos actualizados**: Títulos, inputs, botones, headers usan variables CSS
- **Pendiente**: Continuar reemplazando valores hardcoded por variables

### ⏳ Contenidos/porCategoria.cshtml
- **Estado**: Pendiente
- **Acción**: Agregar link al CSS y actualizar colores/tamaños

### ⏳ Home/Index.cshtml  
- **Estado**: Necesita revisión (redirige a /Home)
- **Acción**: Buscar archivo Home/Index o el destino real

---

## 🔄 Pasos para Aplicar a Otras Páginas

### 1. Agregar el CSS
```razor
@section Styles {
    <link rel="stylesheet" href="~/css/contenidos-cards.css" />
    <style>
        /* Estilos específicos de la página aquí */
    </style>
}
```

### 2. Reemplazar Valores Hardcoded

#### Antes:
```css
.se-page-title {
    font-size: 2.1rem;
    color: #172849;
    padding-bottom: 20px;
}
```

#### Después:
```css
.se-page-title {
    font-size: var(--font-size-4xl);
    color: var(--color-heading);
    padding-bottom: var(--space-lg);
}
```

### 3. Actualizar Colores Morados

#### Antes:
```css
color: #6a4e7a;  /* Morado viejo */
background: #764ba2;
```

#### Después:
```css
color: var(--color-primary);  /* #7c3aed */
background: var(--color-primary);
```

### 4. Espaciado Consistente

#### Antes:
```css
padding: 12px 16px;
margin-bottom: 20px;
gap: 8px;
```

#### Después:
```css
padding: var(--space-sm) var(--space-md);
margin-bottom: var(--space-lg);
gap: var(--space-sm);
```

---

## 📋 Checklist de Actualización

### Para cada página de cards:

- [ ] Agregar `<link>` a `contenidos-cards.css`
- [ ] Reemplazar tamaños de fuente con variables
- [ ] Actualizar colores morados a `--color-primary`
- [ ] Cambiar colores de texto a variables semánticas
- [ ] Reemplazar valores de padding/margin con `--space-*`
- [ ] Actualizar borders con `--color-border`
- [ ] Verificar responsive (variables se adaptan automáticamente)

---

## 🎯 Beneficios

✅ **Consistencia**: Todos los cards usan el mismo sistema  
✅ **Mantenibilidad**: Cambiar una variable afecta todo  
✅ **Responsive**: Variables fluidas se adaptan automáticamente  
✅ **Morado oficial**: Color de marca unificado (`#7c3aed`)  
✅ **Legibilidad**: Tamaños de fuente optimizados  

---

## ⚠️ Notas Importantes

1. **No tocar estilos Admin**: Solo aplicar en páginas públicas
2. **Mantener estilos específicos**: Cards pueden tener estilos únicos además de las variables
3. **Testing**: Revisar en móvil después de aplicar cambios
4. **Morado oficial**: Siempre usar `var(--color-primary)` en vez de colores hardcoded

---

## 🚀 Próximos Pasos

1. ✅ Terminar de actualizar `Contenidos/Index.cshtml`
2. ⏳ Aplicar a `Contenidos/porCategoria.cshtml`
3. ⏳ Encontrar y actualizar la página Home real
4. ⏳ Testing completo en todos los breakpoints
5. ⏳ Documentar cualquier estilo específico que necesite conservarse

---

**Sistema Tipográfico v1.0** - Febrero 2025
