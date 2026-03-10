# Cambio de Modal a Panel Lateral (Side Panel)

## 1. ESTRUCTURA HTML RECOMENDADA

El layout será:

```
┌─────────────────────────────────────────────────┐
│  [GRID DE SÍNTOMAS/TRATAMIENTOS] │ [FORM PANEL] │
│                                   │              │
│                                   │  • Campo 1   │
│  • Fila 1                         │  • Campo 2   │
│  • Fila 2                         │  • Campo 3   │
│  • Fila 3                         │  • Botones   │
│  • Fila 4                         │              │
│  • Fila 5                         │              │
│                                   │              │
└─────────────────────────────────────────────────┘
```

## 2. CSS PARA EL PANEL LATERAL

Agrega a tu CSS (o en un `<style>` en la página):

```css
/* Container principal con flexbox */
.admin-container-flex {
    display: flex;
    gap: 20px;
    height: calc(100vh - 200px);
    overflow: hidden;
}

/* Grid ocupará 65% */
.grid-wrapper {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    overflow-y: auto;
    border-radius: 8px;
    background: white;
    padding: 20px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

/* Panel lateral ocupará 35% */
.side-panel {
    width: 35%;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    padding: 20px;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
}

/* Header del panel */
.side-panel-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 2px solid #e5e7eb;
    padding-bottom: 15px;
    margin-bottom: 20px;
}

.side-panel-header h3 {
    margin: 0;
    font-size: 1.25rem;
    font-weight: 600;
    color: #1f2937;
}

.side-panel-close {
    background: none;
    border: none;
    font-size: 1.5rem;
    color: #6b7280;
    cursor: pointer;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 32px;
    height: 32px;
}

.side-panel-close:hover {
    background: #f3f4f6;
    border-radius: 6px;
}

/* Contenido del formulario */
.side-panel-content {
    flex: 1;
    overflow-y: auto;
}

.side-panel-content form {
    display: flex;
    flex-direction: column;
    gap: 15px;
}

.side-panel-content .form-group {
    display: flex;
    flex-direction: column;
}

.side-panel-content label {
    font-weight: 500;
    color: #374151;
    margin-bottom: 5px;
    font-size: 0.95rem;
}

.side-panel-content input[type="text"],
.side-panel-content input[type="email"],
.side-panel-content textarea,
.side-panel-content select {
    padding: 8px 12px;
    border: 1px solid #d1d5db;
    border-radius: 6px;
    font-size: 0.95rem;
    font-family: inherit;
}

.side-panel-content textarea {
    resize: vertical;
    min-height: 100px;
    max-height: 300px;
}

.side-panel-content input:focus,
.side-panel-content textarea:focus,
.side-panel-content select:focus {
    outline: none;
    border-color: #7c3aed;
    box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
}

/* Checkboxes */
.form-check {
    display: flex;
    align-items: center;
    gap: 8px;
}

.form-check input[type="checkbox"] {
    width: 18px;
    height: 18px;
    cursor: pointer;
}

.form-check label {
    margin: 0;
    cursor: pointer;
}

/* Footer con botones */
.side-panel-footer {
    display: flex;
    gap: 10px;
    border-top: 1px solid #e5e7eb;
    padding-top: 15px;
    margin-top: 20px;
}

.side-panel-footer button {
    flex: 1;
    padding: 10px;
    border-radius: 6px;
    border: none;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s;
}

.side-panel-footer .btn-primary {
    background: #7c3aed;
    color: white;
}

.side-panel-footer .btn-primary:hover {
    background: #6d28d9;
}

.side-panel-footer .btn-secondary {
    background: #f3f4f6;
    color: #374151;
}

.side-panel-footer .btn-secondary:hover {
    background: #e5e7eb;
}

/* Estado vacío */
.side-panel-empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100%;
    color: #9ca3af;
    text-align: center;
}

.side-panel-empty i {
    font-size: 3rem;
    margin-bottom: 10px;
}

/* Responsive: En tablets pequeños */
@media (max-width: 1024px) {
    .admin-container-flex {
        flex-direction: column;
        height: auto;
    }

    .grid-wrapper,
    .side-panel {
        width: 100%;
    }

    .side-panel {
        max-height: 500px;
    }
}
```

## 3. HTML PARA LA PÁGINA (Index de Síntomas/Tratamientos)

```html
<div class="admin-container-flex">
    <!-- Grid de Síntomas/Tratamientos -->
    <div class="grid-wrapper">
        <h2>Gestión de Síntomas</h2>
        <!-- Tu tabla DataTable aquí -->
        <table id="sintomasGrid" class="table">
            <!-- Headers y datos -->
        </table>
    </div>

    <!-- Panel Lateral de Edición -->
    <div class="side-panel" id="editPanel" style="display: none;">
        <div class="side-panel-header">
            <h3 id="panelTitle">Editar Síntoma</h3>
            <button type="button" class="side-panel-close" onclick="closeSidePanel()">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>

        <div class="side-panel-content">
            <form id="editForm">
                <input type="hidden" id="itemId" name="id">

                <div class="form-group">
                    <label for="itemNombre">Nombre</label>
                    <input type="text" id="itemNombre" name="nombre" required>
                </div>

                <div class="form-group">
                    <label for="itemIcono">Icono (opcional)</label>
                    <input type="text" id="itemIcono" name="icono">
                </div>

                <div class="form-group">
                    <label for="DescripcionIA">Descripción IA</label>
                    <textarea id="DescripcionIA" name="DescripcionIA" readonly></textarea>
                    <button type="button" class="btn btn-sm btn-outline-primary mt-2" id="btnGenerarDescripcionIA" style="margin-top: 10px;">
                        <i class="bi bi-sparkles"></i> Generar Descripción IA
                    </button>
                </div>

                <div class="form-check">
                    <input type="checkbox" id="ValidadoIA" name="ValidadoIA">
                    <label for="ValidadoIA">Validado por IA</label>
                </div>

                <div class="form-check">
                    <input type="checkbox" id="ValidadoHumano" name="ValidadoHumano">
                    <label for="ValidadoHumano">Validado por Humano</label>
                </div>

                <div class="form-group">
                    <label for="RelacionEII">Relación con EII</label>
                    <input type="text" id="RelacionEII" name="RelacionEII" readonly>
                </div>

                <!-- Otros campos según sea necesario -->
            </form>
        </div>

        <div class="side-panel-footer">
            <button type="button" class="btn-secondary" onclick="closeSidePanel()">Cancelar</button>
            <button type="button" class="btn-primary" onclick="saveItem()">Guardar</button>
        </div>
    </div>

    <!-- Estado vacío del panel -->
    <div class="side-panel" id="emptyPanel" style="display: flex;">
        <div class="side-panel-empty">
            <i class="bi bi-inbox"></i>
            <p>Selecciona un elemento para editar</p>
        </div>
    </div>
</div>
```

## 4. JAVASCRIPT PARA CONTROLAR EL PANEL

```javascript
function openEditPanel(id, tipo = 'sintoma') {
    // Cargar datos del item
    fetch(`/api/admin/${tipo}s/${id}`)
        .then(r => r.json())
        .then(data => {
            // Llenar formulario
            document.getElementById('itemId').value = data.id;
            document.getElementById('itemNombre').value = data.nombre;
            document.getElementById('itemIcono').value = data.icono || '';
            document.getElementById('DescripcionIA').value = data.descripcionIA || '';
            document.getElementById('ValidadoIA').checked = data.validadoIA || false;
            document.getElementById('ValidadoHumano').checked = data.validadoHumano || false;
            document.getElementById('RelacionEII').value = data.relacionEII || '';

            // Mostrar panel
            document.getElementById('emptyPanel').style.display = 'none';
            document.getElementById('editPanel').style.display = 'flex';
        });
}

function closeSidePanel() {
    document.getElementById('editPanel').style.display = 'none';
    document.getElementById('emptyPanel').style.display = 'flex';
    document.getElementById('editForm').reset();
}

function saveItem() {
    const id = document.getElementById('itemId').value;
    const formData = {
        nombre: document.getElementById('itemNombre').value,
        icono: document.getElementById('itemIcono').value,
        descripcionIA: document.getElementById('DescripcionIA').value,
        validadoIA: document.getElementById('ValidadoIA').checked,
        validadoHumano: document.getElementById('ValidadoHumano').checked,
        relacionEII: document.getElementById('RelacionEII').value
    };

    fetch(`/api/admin/sintomas/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData)
    })
    .then(r => r.json())
    .then(data => {
        if (data.ok) {
            alert('✅ Guardado correctamente');
            closeSidePanel();
            // Recargar grid
            reloadGrid();
        }
    });
}
```

## 5. MODIFICAR BOTON DE EDITAR EN EL GRID

Cambiar:
```javascript
// De:
openEditModal(id);

// A:
openEditPanel(id);
```

---

## 6. VENTAJAS DE ESTE ENFOQUE

✅ Más espacio para el grid  
✅ Panel siempre visible y accesible  
✅ No interrumpe la vista del grid  
✅ Mejor UX en pantallas grandes  
✅ Responsive a tablets/móviles  
✅ Más profesional y moderno

