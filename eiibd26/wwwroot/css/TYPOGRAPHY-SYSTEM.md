# 📖 Sistema Tipográfico EIIBD
## Design System para Lectura Larga

### 🎯 Filosofía de Diseño

Este sistema tipográfico está diseñado específicamente para **artículos de lectura larga** sobre salud y conocimiento, inspirado en las mejores prácticas de plataformas como:
- **Medium** - Tipografía fluida y espaciado generoso
- **Substack** - Jerarquía clara y legibilidad móvil
- **Notion** - Sistema escalable y mantenible
- **Stripe Docs** - Claridad y profesionalismo

---

## 🎨 Design Tokens (CSS Variables)

### Tipografía Base
```css
--font-primary: 'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif
--font-size-base: clamp(1rem, 0.9rem + 0.25vw, 1.125rem)  /* 16-18px */
--line-height-base: 1.7  /* Óptimo para lectura prolongada */
--content-max-width: 68ch  /* 60-75 caracteres por línea */
```

### Escala Tipográfica (Perfect Fourth: 1.333)
```css
--font-size-xs: 0.75-0.875rem   /* 12-14px */
--font-size-sm: 0.875-1rem      /* 14-16px */
--font-size-md: 1-1.125rem      /* 16-18px - BASE */
--font-size-lg: 1.125-1.25rem   /* 18-20px */
--font-size-xl: 1.25-1.5rem     /* 20-24px */
--font-size-2xl: 1.5-2rem       /* 24-32px */
--font-size-3xl: 1.875-2.5rem   /* 30-40px */
--font-size-4xl: 2.25-3rem      /* 36-48px */
```

### Colores Oficiales (Morado)
```css
--color-primary: #7c3aed         /* Morado principal */
--color-primary-hover: #6d28d9   /* Hover state */
--color-primary-light: #a78bfa   /* Decoraciones */
--color-primary-bg: #f3e8ff      /* Fondos suaves */
```

### Colores de Texto (Reducción de Fatiga Visual)
```css
--color-text-primary: #1f2937    /* En vez de #000 */
--color-text-secondary: #6b7280  /* Metadata, captions */
--color-text-tertiary: #9ca3af   /* Elementos terciarios */
--color-heading: #111827         /* Headings */
```

---

## 📐 Decisiones UX

### 1. **Tipografía Fluida con `clamp()`**
```css
font-size: clamp(1rem, 0.9rem + 0.25vw, 1.125rem);
```
**Por qué:** Escala suavemente entre dispositivos sin media queries, manteniendo legibilidad en todos los tamaños de pantalla.

### 2. **Ancho Óptimo de Lectura (68ch)**
```css
max-width: 68ch;  /* ~60-75 caracteres por línea */
```
**Por qué:** La investigación muestra que 60-75 caracteres por línea es óptimo para comprensión y velocidad de lectura.

### 3. **Interlineado 1.7**
```css
line-height: 1.7;
```
**Por qué:** Mayor que el estándar (1.5) para reducir fatiga en lectura prolongada. Mejora la distinguibilidad entre líneas.

### 4. **Espaciado Generoso**
```css
margin-bottom: var(--space-xl);  /* 2rem */
```
**Por qué:** El espacio en blanco reduce la carga cognitiva y mejora la jerarquía visual.

### 5. **Jerarquía de Headings**
```css
h1: 36-48px (font-size-4xl)
h2: 30-40px (font-size-3xl) + border-bottom
h3: 24-32px (font-size-2xl)
h4: 20-24px (font-size-xl)
```
**Por qué:** Diferenciación clara entre niveles. El border-bottom en H2 mejora la escanabilidad.

### 6. **Enlaces con Subrayado Personalizado**
```css
text-decoration-color: var(--color-primary-light);
text-decoration-thickness: 0.1em;
text-underline-offset: 0.15em;
```
**Por qué:** Subrayado más sutil y elegante que el navegador predeterminado. Color morado mantiene la identidad de marca.

### 7. **Párrafo Introductorio Destacado**
```css
.contenido-html p:first-of-type {
    font-size: var(--font-size-lg);
    color: var(--color-text-secondary);
}
```
**Por qué:** Patrón común en periodismo digital. Engancha al lector y establece contexto.

### 8. **Blockquotes Distintivos**
```css
border-left: 4px solid var(--color-primary);
background: var(--color-primary-bg);
```
**Por qué:** Color morado refuerza identidad de marca. Fondo diferencia claramente citas del contenido principal.

---

## 🚀 Cómo Usar

### En HTML del Artículo
```html
<div class="contenido-html">
    <h1>Título del Artículo</h1>
    <p>Párrafo introductorio más grande...</p>
    <h2>Sección Principal</h2>
    <p>Contenido regular...</p>
    <!-- El sistema se aplica automáticamente -->
</div>
```

### Personalización de Variables
```css
/* En tu CSS, puedes sobrescribir: */
:root {
    --color-primary: #tu-color;
    --font-size-base: clamp(...);
}
```

---

## 📱 Responsive Behavior

### Móvil (< 768px)
- Font size: **16px** (mínimo legible)
- Padding reducido: `1rem`
- Espaciado vertical ajustado

### Tablet (768px - 1024px)
- Font size: **17px** (transición)
- Padding: `1.5rem`

### Desktop (> 1024px)
- Font size: **18px** (óptimo)
- Padding: `2rem`
- Máxima anchura: `68ch`

---

## ✅ Checklist de Legibilidad

- [x] Tamaño mínimo 16px en móvil
- [x] Interlineado 1.6-1.8
- [x] 60-75 caracteres por línea
- [x] Sin negro puro (#000)
- [x] Jerarquía clara H1-H6
- [x] Enlaces distinguibles
- [x] Espaciado generoso
- [x] Tipografía fluida
- [x] Colores de marca (morado)
- [x] Optimizado para móvil first

---

## 🎨 Paleta de Colores

### Morado (Marca)
- `#7c3aed` - Primary
- `#6d28d9` - Hover
- `#a78bfa` - Light
- `#f3e8ff` - Background

### Texto
- `#1f2937` - Primary
- `#6b7280` - Secondary
- `#9ca3af` - Tertiary
- `#111827` - Headings

---

## 🔧 Mantenimiento

### Añadir Nuevo Tamaño
```css
--font-size-custom: clamp(minimo, fluido, maximo);
```

### Cambiar Color Principal
```css
:root {
    --color-primary: #nuevo-color;
}
```

### Ajustar Espaciado
```css
--space-custom: 2.5rem;
```

---

## 📊 Métricas de Éxito

Este sistema está optimizado para:
- ✅ **Tiempo de lectura aumentado** (espaciado cómodo)
- ✅ **Menor tasa de rebote** (mejor legibilidad)
- ✅ **Mayor comprensión** (jerarquía clara)
- ✅ **Accesibilidad mejorada** (contraste WCAG AA+)
- ✅ **Experiencia móvil superior** (16px+ base)

---

## 🌟 Inspiración y Referencias

- [Medium Typography](https://medium.design/)
- [Practical Typography](https://practicaltypography.com/)
- [Butterick's Practical Typography](https://practicaltypography.com/)
- [The Elements of Typographic Style](https://www.amazon.com/Elements-Typographic-Style-Robert-Bringhurst/dp/0881791326)
- [Web Typography](https://webtypography.net/)

---

**Creado para EIIBD** - Sistema tipográfico v1.0
