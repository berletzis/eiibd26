# 🚀 Guía Rápida de Integración

## ✅ Sistema Tipográfico Ya Implementado

El sistema tipográfico está **completamente integrado** en `detalle.css`. Solo necesitas usar las clases correctas.

---

## 📝 Para Nuevas Páginas de Contenido

### 1. Estructura HTML Básica
```html
<article class="contenido-html">
    <!-- Tu contenido aquí -->
</article>
```

### 2. Elementos Soportados
Todos estos elementos ya tienen estilos optimizados:

```html
<h1>Título Principal</h1>
<h2>Sección</h2>
<h3>Subsección</h3>
<h4>Título Menor</h4>

<p>Párrafo normal con espaciado perfecto.</p>

<a href="#">Enlaces con estilo morado</a>

<strong>Texto en negrita</strong>
<em>Texto en cursiva</em>

<ul>
    <li>Lista no ordenada</li>
    <li>Con bullets morados</li>
</ul>

<ol>
    <li>Lista ordenada</li>
    <li>Con números morados</li>
</ol>

<blockquote>
    Cita destacada con fondo morado suave
    <cite>— Autor</cite>
</blockquote>

<code>código inline</code>

<pre><code>
// Bloque de código
function ejemplo() {
    return "con sintaxis";
}
</code></pre>

<hr> <!-- Línea separadora -->

<img src="..." alt="...">
<figure>
    <img src="..." alt="...">
    <figcaption>Descripción de la imagen</figcaption>
</figure>

<table>
    <thead>
        <tr><th>Columna 1</th><th>Columna 2</th></tr>
    </thead>
    <tbody>
        <tr><td>Dato 1</td><td>Dato 2</td></tr>
    </tbody>
</table>
```

---

## 🎨 Personalizar Colores (Opcional)

### Cambiar el Color Primario
```css
:root {
    --color-primary: #tu-morado;
    --color-primary-hover: #tu-morado-oscuro;
}
```

### Cambiar Tamaño de Texto Base
```css
:root {
    --font-size-base: clamp(1rem, 0.9rem + 0.3vw, 1.2rem);
}
```

---

## 📱 Ya Incluido (No Hacer Nada)

✅ **Responsive automático** (móvil → desktop)  
✅ **Tipografía fluida** con `clamp()`  
✅ **Colores morados oficiales**  
✅ **Espaciado óptimo**  
✅ **Jerarquía clara**  
✅ **Enlaces accesibles**  
✅ **Listas estilizadas**  
✅ **Blockquotes con marca**  
✅ **Código con highlight**  

---

## 🛠️ Personalizar para Casos Especiales

### Artículo Ancho Completo
```css
.contenido-html.full-width {
    max-width: 100%;
}
```

### Tamaño de Texto Más Grande
```css
.contenido-html.large-text {
    --font-size-base: clamp(1.125rem, 1rem + 0.5vw, 1.25rem);
}
```

### Espaciado Compacto
```css
.contenido-html.compact {
    --space-xl: 1rem;
    --space-2xl: 1.5rem;
}
```

---

## 🎯 Ejemplo Completo

```html
<div class="conte-detail">
    <div class="page-title">
        <h1>Título de la Página</h1>
    </div>

    <div class="meta-card">
        <div class="author">
            <img src="/path/avatar.jpg" alt="Autor">
            <div>
                <div>por Nombre Autor | 15 de febrero | 5 min read</div>
            </div>
        </div>
    </div>

    <article class="content-panel">
        <div class="contenido-html">
            <p>Párrafo introductorio más grande...</p>
            <h2>Primera Sección</h2>
            <p>Contenido normal...</p>
            <ul>
                <li>Lista item 1</li>
                <li>Lista item 2</li>
            </ul>
            <blockquote>
                Cita importante
                <cite>— Fuente</cite>
            </blockquote>
            <p>Más contenido con <a href="#">enlaces</a> y <strong>énfasis</strong>.</p>
        </div>
    </article>
</div>
```

---

## ⚠️ No Aplicar A:

- ❌ Admin dashboard (`/Identity/Pages/Admin/`)
- ❌ Panel de usuario (`/Identity/Pages/Usuario/`)
- ❌ Formularios de login/registro
- ❌ Tablas de datos administrativos

**Solo aplicar a:** Páginas públicas de contenido (artículos, preguntas, tips, etc.)

---

## 🆘 Troubleshooting

### "El texto se ve muy pequeño en móvil"
✅ **Solución:** Ya está arreglado con `clamp(1rem, ...)`. Mínimo 16px.

### "Quiero cambiar el ancho máximo"
```css
:root {
    --content-max-width: 75ch; /* Ajusta según necesites */
}
```

### "Los enlaces no se ven morados"
✅ **Solución:** Asegúrate de que los enlaces estén dentro de `.contenido-html`

### "Necesito más espacio entre párrafos"
```css
.contenido-html p {
    margin-bottom: var(--space-2xl); /* o 3rem directo */
}
```

---

## 📞 Recursos

- `TYPOGRAPHY-SYSTEM.md` - Documentación completa
- `typography-example.html` - Ejemplos visuales
- `detalle.css` - Código fuente del sistema

---

**¡Listo para usar!** 🎉
