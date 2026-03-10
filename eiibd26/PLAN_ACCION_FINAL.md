# PLAN DE ACCIÓN - Implementar Mejoras Síntomas/Tratamientos

## 🎯 OBJETIVO FINAL

Agregar capacidades de generación de descripciones por IA a los administradores de Síntomas y Tratamientos, con cambio de UI de Modal a Panel Lateral.

---

## 📅 TIMELINE ESTIMADO

- **Fase 1**: 10 minutos (migraciones EF Core)
- **Fase 2**: 20 minutos (crear endpoint API)
- **Fase 3**: 30 minutos (actualizar HTML/CSS)
- **Fase 4**: 15 minutos (JavaScript para panel lateral)
- **Fase 5**: 15 minutos (testing y ajustes)

**TOTAL**: ~90 minutos

---

## ✅ PASO A PASO

### PASO 1: Ejecutar SQL Queries (10 min)

**OPCIÓN A: Ejecutar manualmente en SQL Server (Recomendado)**

1. Abre **SQL Server Management Studio** o **Azure Data Studio**

2. Sigue la guía: **GUIA_EJECUCION_SQL.md**
   - Ejecuta cada PASO por separado
   - Verifica que no hay errores antes de continuar al siguiente

3. Verifica al final:
   ```sql
   SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
   WHERE TABLE_NAME IN ('sintomas', 'tratamientos')
   ORDER BY TABLE_NAME, ORDINAL_POSITION;
   ```

**OPCIÓN B: Ejecutar con EF Core Migrations (Alternativa)**

1. Abre **Package Manager Console** en Visual Studio
   ```powershell
   # View > Other Windows > Package Manager Console
   ```

2. Ejecuta:
   ```powershell
   Add-Migration AgregaSintomasYTratamientosIA
   Update-Database
   ```

✅ **Checkpoint**: Las nuevas columnas y tablas existen en la DB

---

### PASO 2: Crear Controller API (20 min)

1. **Crea archivo**: `eiibd26/Controllers/AdminSintomasTratamientosApiController.cs`
   - Copia contenido de `IMPLEMENTACION_ENDPOINT_COMPLETO.md`
   - Actualiza using statements según tu proyecto

2. **Verifica que compila**:
   ```powershell
   Build-Solution  # o Ctrl+Shift+B
   ```

✅ **Checkpoint**: El endpoint compila sin errores

---

### PASO 3: Actualizar Views (30 min)

#### 3.1 Actualizar `Index.cshtml` de Síntomas

Archivo: `eiibd26/Areas/Identity/Pages/Admin/Sintomas/Index.cshtml`

Cambios:

```html
<!-- AGREGAR CONTENEDOR FLEX -->
<div class="admin-container-flex">
    <!-- Grid -->
    <div class="grid-wrapper">
        <!-- Tu tabla DataTable aquí -->
        <table id="sintomasGrid" class="table">
            <!-- ... -->
        </table>
    </div>

    <!-- Panel Lateral -->
    <div class="side-panel" id="editPanel" style="display: none;">
        <!-- Copiar HTML de CAMBIO_MODAL_A_PANEL_LATERAL.md -->
    </div>

    <!-- Panel Vacío -->
    <div class="side-panel" id="emptyPanel">
        <!-- Copiar HTML de CAMBIO_MODAL_A_PANEL_LATERAL.md -->
    </div>
</div>

<!-- Agregar CSS -->
<style>
    /* Copiar CSS de CAMBIO_MODAL_A_PANEL_LATERAL.md */
</style>
```

#### 3.2 Actualizar grid DataTable

En el JavaScript donde defines las columnas del grid, agrega:

```javascript
{
    data: 'validadoIA',
    render: function(d) {
        return d ? '<i class="bi bi-check-circle-fill text-success"></i>' : '<i class="bi bi-x-circle text-danger"></i>';
    }
},
{
    data: 'validadoHumano',
    render: function(d) {
        return d ? '<i class="bi bi-check-circle-fill text-success"></i>' : '<i class="bi bi-x-circle text-danger"></i>';
    }
},
{
    data: 'relacionEII',
    render: function(d) {
        return d ? d.substring(0, 20) + '...' : '-';
    }
},
{
    data: null,
    render: function(d) {
        return `<button class="btn btn-sm btn-outline-primary" onclick="openEditPanel(${d.id})">
                    <i class="bi bi-pencil"></i> Editar
                </button>`;
    }
}
```

#### 3.3 Actualizar botón de editar

**Antes:**
```javascript
// En el render de "Acciones" del grid
`<button onclick="editModal(${d.id})">Editar</button>`
```

**Después:**
```javascript
`<button onclick="openEditPanel(${d.id})">Editar</button>`
```

#### 3.4 Hacer lo mismo para Tratamientos

Repite pasos 3.1-3.3 para `Identity/Admin/Tratamientos/Index.cshtml`

✅ **Checkpoint**: HTML y estructura están en su lugar

---

### PASO 4: JavaScript para Panel Lateral (15 min)

Agrega al final del archivo Index.cshtml (en sección Scripts):

```html
<script>
function openEditPanel(id, tipo = 'sintoma') {
    console.log('Abriendo panel para', tipo, id);

    // Crear URL según el tipo
    const endpoint = tipo === 'sintoma' ? 'sintomas' : 'tratamientos';

    // Cargar datos
    fetch(`/api/admin/${endpoint}/${id}`)
        .then(r => {
            if (!r.ok) throw new Error(`HTTP error! status: ${r.status}`);
            return r.json();
        })
        .then(data => {
            if (!data.ok) {
                alert('Error: ' + (data.error || 'No se pudo cargar'));
                return;
            }

            const d = data.data;

            // Llenar formulario
            document.getElementById('itemId').value = d.id;
            document.getElementById('itemType').value = tipo;
            document.getElementById('itemNombre').value = d.nombre || '';
            document.getElementById('itemIcono').value = d.icono || '';
            document.getElementById('DescripcionIA').value = d.descripcionIA || '';
            document.getElementById('ValidadoIA').checked = d.validadoIA || false;
            document.getElementById('ValidadoHumano').checked = d.validadoHumano || false;
            document.getElementById('RelacionEII').value = d.relacionEII || '';

            // Actualizar título
            const titulo = tipo === 'sintoma' ? 'Editar Síntoma' : 'Editar Tratamiento';
            document.getElementById('panelTitle').textContent = titulo;

            // Mostrar panel
            document.getElementById('emptyPanel').style.display = 'none';
            document.getElementById('editPanel').style.display = 'flex';

            // Registrar listeners
            setupPanelListeners(tipo);
        })
        .catch(err => {
            console.error('Error:', err);
            alert('Error al cargar datos');
        });
}

function closeSidePanel() {
    document.getElementById('editPanel').style.display = 'none';
    document.getElementById('emptyPanel').style.display = 'flex';
    document.getElementById('editForm').reset();
}

function setupPanelListeners(tipo) {
    const btnIA = document.getElementById('btnGenerarDescripcionIA');
    if (btnIA) {
        // Remover listeners anteriores
        const newBtn = btnIA.cloneNode(true);
        btnIA.parentNode.replaceChild(newBtn, btnIA);

        newBtn.addEventListener('click', async function() {
            await generateIADescription(tipo);
        });
    }
}

async function generateIADescription(tipo) {
    const id = document.getElementById('itemId').value;
    const endpoint = tipo === 'sintoma' ? 'sintomas' : 'tratamientos';
    const btn = document.getElementById('btnGenerarDescripcionIA');

    // Mostrar estado
    btn.disabled = true;
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Generando...';

    try {
        const response = await fetch(`/api/admin/${endpoint}/${id}/generate-ia-description`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const data = await response.json();

        if (data.ok) {
            // Actualizar campos
            document.getElementById('DescripcionIA').value = data.descripcion || '';
            document.getElementById('RelacionEII').value = data.relacionEII || '';
            document.getElementById('ValidadoIA').checked = true;

            alert('✅ Descripción generada y guardada automáticamente');
            
            // Guardar cambios
            await saveItem();
        } else {
            alert('❌ Error: ' + (data.error || 'Error desconocido'));
        }
    } catch (error) {
        console.error('Error:', error);
        alert('❌ Error de conexión');
    } finally {
        btn.disabled = false;
        btn.innerHTML = originalText;
    }
}

async function saveItem() {
    const id = document.getElementById('itemId').value;
    const tipo = document.getElementById('itemType').value;
    const endpoint = tipo === 'sintoma' ? 'sintomas' : 'tratamientos';

    const formData = {
        nombre: document.getElementById('itemNombre').value,
        icono: document.getElementById('itemIcono').value,
        descripcionIA: document.getElementById('DescripcionIA').value,
        validadoIA: document.getElementById('ValidadoIA').checked,
        validadoHumano: document.getElementById('ValidadoHumano').checked,
        relacionEII: document.getElementById('RelacionEII').value
    };

    try {
        const response = await fetch(`/api/admin/${endpoint}/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formData)
        });

        const data = await response.json();

        if (data.ok) {
            console.log('✅ Guardado correctamente');
            // Recargar grid
            if (window.sintomasTable) window.sintomasTable.ajax.reload();
            if (window.tratamientosTable) window.tratamientosTable.ajax.reload();
        }
    } catch (error) {
        console.error('Error al guardar:', error);
    }
}

// Al cargar la página
document.addEventListener('DOMContentLoaded', function() {
    // Registrar listener de cerrar panel
    const btnClose = document.querySelector('.side-panel-close');
    if (btnClose) {
        btnClose.addEventListener('click', closeSidePanel);
    }

    // Registrar listener de guardar
    const btnSave = document.querySelector('.side-panel-footer .btn-primary');
    if (btnSave) {
        btnSave.addEventListener('click', saveItem);
    }
});
</script>
```

✅ **Checkpoint**: Panel lateral responde a clics y carga datos

---

### PASO 5: Testing (15 min)

#### 5.1 Pruebas Básicas

```
[ ] 1. Cargar página de Síntomas/Tratamientos
    [ ] Grid aparece correctamente
    [ ] Panel lateral aparece vacío

[ ] 2. Hacer clic en "Editar"
    [ ] Panel se muestra con datos
    [ ] Formulario está lleno correctamente

[ ] 3. Hacer clic en "Generar Descripción IA"
    [ ] Botón muestra "Generando..."
    [ ] Descripción aparece en el campo
    [ ] RelacionEII se completa

[ ] 4. Hacer cambios y guardar
    [ ] Grid se actualiza
    [ ] Los datos persisten en la BD

[ ] 5. Responsividad
    [ ] Desktop: layout 65/35 se ve bien
    [ ] Tablet: layout cambia a apilado
```

#### 5.2 Verificar en Console del Navegador

Abre Developer Tools (F12) y verifica:
- ❌ Sin errores JavaScript
- ❌ Requests HTTP 200 OK
- ❌ Respuestas JSON válidas

✅ **Checkpoint**: Todo funciona sin errores

---

### PASO 6: Ajustes Finales (Según sea necesario)

- Ajustar ancho del panel (currently 35%)
- Cambiar colores según tu tema
- Agregar más campos si es necesario

---

## 📚 ARCHIVOS DE REFERENCIA

Consulta mientras implementas:

1. **MIGRACION_SINTOMAS_TRATAMIENTOS.md**
   - SQL queries para crear tablas

2. **INSTRUCCIONES_MIGRACION.md**
   - Paso a paso de migraciones EF Core

3. **IMPLEMENTACION_ENDPOINT_COMPLETO.md**
   - Código completo del controller API

4. **CAMBIO_MODAL_A_PANEL_LATERAL.md**
   - CSS y HTML del panel lateral

5. **ENDPOINT_IA_DESCRIPCION.md**
   - Prompts y estructura API

---

## 🚨 TROUBLESHOOTING

### "El endpoint devuelve 404"
→ Verifica que el controller esté en la carpeta `Controllers/`

### "La IA no genera descripción"
→ Verifica que `IClaudeAiService` esté registrado en Startup/Program.cs

### "El panel no aparece"
→ Verifica que el CSS esté incluido y el HTML esté correcto

### "Error: Adding an abstract auto-property..."
→ Reinicia Visual Studio (solo advertencia de Hot Reload)

---

## ✨ PRÓXIMO: (Después de implementar estas mejoras)

- [ ] Agregar notas colaborativas (SintomasNotas/TratamientosNotas)
- [ ] Crear proceso automático VerificadorIA
- [ ] Agregar búsqueda/filtro a grids
- [ ] Exportar datos a Excel

