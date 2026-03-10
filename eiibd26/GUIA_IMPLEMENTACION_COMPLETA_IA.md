# ✅ IMPLEMENTACIÓN COMPLETA - Síntomas y Tratamientos con IA

## 📋 RESUMEN

Se ha implementado **TODO** lo necesario para agregar funcionalidad de IA a Síntomas y Tratamientos:

### ✅ LO QUE YA ESTÁ HECHO

1. **Modelos actualizados**
   - ✅ `Models/sintomas.cs` - Tiene campos: DescripcionIA, ValidadoIA, ValidadoHumano, RelacionEII (bool), RelacionEIIDescripcion
   - ✅ `Models/tratamientos.cs` - Mismos campos que sintomas
   - ✅ `Models/SintomasNotas.cs` - Existe y está completo
   - ✅ `Models/TratamientosNotas.cs` - Existe y está completo

2. **Servicios de IA creados**
   - ✅ `Services/AI/ISintomasTratamientosAiService.cs` - Interface
   - ✅ `Services/AI/SintomasTratamientosAiService.cs` - Implementación completa con Claude
   - ✅ Reutiliza `AiAnswerService` existente y configuración de Claude

3. **Controllers API creados**
   - ✅ `Controllers/SintomasAdminController.cs` - Endpoints GET/PUT y generar descripción IA
   - ✅ `Controllers/TratamientosAdminController.cs` - Endpoints GET/PUT y generar descripción IA

4. **SQL Queries**
   - ✅ `Migrations/SQL_UPDATE_SINTOMAS_TRATAMIENTOS_IA.sql` - Script completo para actualizar la BD

---

## 🚀 PASOS PARA TERMINAR LA IMPLEMENTACIÓN

### PASO 1: Ejecutar SQL en la Base de Datos

1. Abre SQL Server Management Studio
2. Ejecuta el archivo: `Migrations/SQL_UPDATE_SINTOMAS_TRATAMIENTOS_IA.sql`
3. Verifica que NO haya errores

```sql
-- Verificar que las columnas existen
SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'sintomas' 
AND COLUMN_NAME IN ('RelacionEII', 'RelacionEIIDescripcion');
```

### PASO 2: Registrar el Servicio en Program.cs

Abre `Program.cs` y después de la línea donde dice:

```csharp
builder.Services.AddSingleton<eiibd26.Services.AI.IAiAnswerService, eiibd26.Services.AI.AiAnswerService>();
```

**Agrega esta línea:**

```csharp
builder.Services.AddScoped<eiibd26.Services.AI.ISintomasTratamientosAiService, eiibd26.Services.AI.SintomasTratamientosAiService>();
```

### PASO 3: Compilar el proyecto

```bash
dotnet build
```

Si hay errores, revísalos y corrígelos.

### PASO 4: Actualizar la Vista de Síntomas (Index.cshtml)

Necesitas:

1. **Cambiar el modal por un panel lateral**
2. **Agregar columnas nuevas al grid** (ValidadoIA, ValidadoHumano, RelacionEII)
3. **Agregar botón "Generar Descripción IA"**
4. **Agregar campo Descripción IA en el formulario**

---

## 📝 CAMBIOS PENDIENTES EN EL FRONTEND

### A. Modificar `Areas/Identity/Pages/Admin/Sintomas/Index.cshtml`

#### 1. Agregar CSS para panel lateral

Agrega al inicio del archivo, dentro de `@section Styles`:

```css
/* Panel lateral en lugar de modal */
.admin-container-flex {
    display: flex;
    gap: 20px;
    min-height: calc(100vh - 250px);
}

.grid-wrapper {
    flex: 1 1 65%;
    min-width: 0;
    background: white;
    border-radius: 12px;
    padding: 20px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.08);
}

.side-panel {
    flex: 0 0 35%;
    background: white;
    border-radius: 12px;
    padding: 24px;
    box-shadow: 0 2px 12px rgba(0,0,0,0.1);
    max-height: calc(100vh - 250px);
    overflow-y: auto;
    display: none; /* Oculto por defecto */
}

.side-panel.active {
    display: block;
}

.side-panel-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 2px solid #e5e7eb;
    padding-bottom: 16px;
    margin-bottom: 24px;
}

.side-panel-header h3 {
    margin: 0;
    font-size: 1.35rem;
    font-weight: 600;
    color: #1f2937;
}

.btn-close-panel {
    background: #f3f4f6;
    border: none;
    width: 32px;
    height: 32px;
    border-radius: 8px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.25rem;
    color: #6b7280;
}

.btn-close-panel:hover {
    background: #e5e7eb;
    color: #374151;
}

.btn-generar-ia {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border: none;
    padding: 10px 20px;
    border-radius: 8px;
    font-weight: 600;
    margin-top: 12px;
    cursor: pointer;
    transition: all 0.2s;
}

.btn-generar-ia:hover {
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.btn-generar-ia:disabled {
    background: #9ca3af;
    cursor: not-allowed;
    transform: none;
}

.loading-spinner {
    display: inline-block;
    width: 16px;
    height: 16px;
    border: 3px solid rgba(255,255,255,.3);
    border-radius: 50%;
    border-top-color: #fff;
    animation: spinner 0.6s linear infinite;
}

@keyframes spinner {
    to { transform: rotate(360deg); }
}
```

#### 2. Modificar el HTML del grid/formulario

Reemplaza todo el contenido dentro de `<div class="crm-container-admin">` con:

```html
<div class="crm-container-admin">
    <h2 class="crm-title-admin mb-4">Síntomas</h2>
    
    <div class="admin-container-flex">
        <!-- Grid (lado izquierdo) -->
        <div class="grid-wrapper">
            <div class="mb-3">
                <div class="form-check form-switch d-inline-block">
                    <input class="form-check-input" type="checkbox" role="switch" id="switchEliminadosSintomas">
                    <label class="form-check-label" for="switchEliminadosSintomas">Mostrar eliminados</label>
                </div>
            </div>
            
            <table id="sintomasGrid" class="table table-bordered table-hover align-middle mb-0">
                <thead>
                    <tr>
                        <th>Nombre</th>
                        <th>Tipo</th>
                        <th>ID Padre</th>
                        <th>Idioma</th>
                        <th>✓ IA</th>
                        <th>✓ Humano</th>
                        <th>EII</th>
                        <th>Eliminado</th>
                        <th>Acciones</th>
                    </tr>
                </thead>
            </table>
        </div>

        <!-- Panel lateral (lado derecho) -->
        <div class="side-panel" id="panelEditarSintoma">
            <div class="side-panel-header">
                <h3>Editar Síntoma</h3>
                <button type="button" class="btn-close-panel" id="btnCerrarPanel">
                    <i class="bi bi-x-lg"></i>
                </button>
            </div>

            <form id="formEditarSintoma">
                <input type="hidden" name="id" id="editSintoma_id">
                
                <div class="mb-3">
                    <label for="editSintoma_nombre" class="form-label">Nombre</label>
                    <input type="text" class="form-control" id="editSintoma_nombre" name="nombre" required>
                </div>
                
                <div class="mb-3">
                    <label for="editSintoma_idPadre" class="form-label">ID Padre (vacío si es padre)</label>
                    <input type="number" class="form-control" id="editSintoma_idPadre" name="idPadre">
                </div>
                
                <div class="mb-3">
                    <label for="editSintoma_idIdioma" class="form-label">ID Idioma</label>
                    <input type="number" class="form-control" id="editSintoma_idIdioma" name="idIdioma" required>
                </div>
                
                <div class="mb-3">
                    <label for="editSintoma_icono" class="form-label">Ícono</label>
                    <input type="text" class="form-control" id="editSintoma_icono" name="icono">
                </div>

                <!-- NUEVO: Descripción IA -->
                <div class="mb-3">
                    <label for="editSintoma_descripcionIA" class="form-label">
                        Descripción IA
                        <button type="button" class="btn btn-generar-ia btn-sm" id="btnGenerarIA">
                            <i class="bi bi-magic"></i> Generar con IA
                        </button>
                    </label>
                    <textarea class="form-control" id="editSintoma_descripcionIA" name="descripcionIA" rows="5" placeholder="Presiona 'Generar con IA' para crear una descripción automática..."></textarea>
                </div>

                <!-- NUEVO: Validación humana -->
                <div class="mb-3 form-check">
                    <input type="checkbox" class="form-check-input" id="editSintoma_validadoHumano" name="validadoHumano">
                    <label class="form-check-label" for="editSintoma_validadoHumano">
                        ✓ Validado por Humano
                    </label>
                </div>

                <!-- NUEVO: Relación con EII (solo lectura, lo llena la IA) -->
                <div class="mb-3">
                    <label class="form-label">Relación con EII</label>
                    <input type="text" class="form-control" id="editSintoma_relacionEII" readonly>
                </div>
                
                <div class="mb-3">
                    <label for="editSintoma_eliminado" class="form-label">¿Eliminado?</label>
                    <select class="form-select" id="editSintoma_eliminado" name="eliminado">
                        <option value="false">No</option>
                        <option value="true">Sí</option>
                    </select>
                </div>

                <div class="alert alert-success d-none" id="msgEditSintomaSuccess">
                    ¡Guardado exitosamente!
                </div>

                <div class="d-flex gap-2">
                    <button type="submit" class="btn btn-primary flex-grow-1">
                        <i class="bi bi-check-lg"></i> Guardar Cambios
                    </button>
                    <button type="button" class="btn btn-secondary" id="btnCancelar">
                        Cancelar
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>
```

#### 3. Actualizar JavaScript

En `@section Scripts`, reemplaza TODO el JavaScript con:

```javascript
<script>
let sintomasTabla;
let hijosSintPorPadre = {};
let sintomaActualId = null;

function actualizarDiccionarioHijosSintomas(data) {
    hijosSintPorPadre = {};
    data.forEach(item => {
        if (item.idPadre && item.idPadre !== 0) {
            if (!hijosSintPorPadre[item.idPadre]) {
                hijosSintPorPadre[item.idPadre] = [];
            }
            hijosSintPorPadre[item.idPadre].push(item.id);
        }
    });
}

function cerrarPanel() {
    $('#panelEditarSintoma').removeClass('active');
    sintomaActualId = null;
}

function abrirPanel(id) {
    sintomaActualId = id;
    $('#panelEditarSintoma').addClass('active');
    
    // Cargar datos del síntoma
    $.get(`/api/admin/sintomas/${id}`, function(response) {
        if (response.ok) {
            $('#editSintoma_id').val(response.id);
            $('#editSintoma_nombre').val(response.nombre);
            $('#editSintoma_idPadre').val(response.idPadre || '');
            $('#editSintoma_idIdioma').val(response.idIdioma);
            $('#editSintoma_icono').val(response.icono);
            $('#editSintoma_eliminado').val(response.eliminado ? 'true' : 'false');
            $('#editSintoma_descripcionIA').val(response.descripcionIA);
            $('#editSintoma_validadoHumano').prop('checked', response.validadoHumano);
            $('#editSintoma_relacionEII').val(
                response.relacionEII 
                    ? '✅ Sí, documentada' 
                    : '❌ No documentada'
            );
            $('#msgEditSintomaSuccess').addClass('d-none');
        }
    }).fail(function() {
        alert('Error al cargar los datos del síntoma');
    });
}

$(function () {
    // Inicializar DataTable
    sintomasTabla = $('#sintomasGrid').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '@Url.Page(null, "GridData")',
            type: 'GET',
            data: function (d) {
                d.mostrarEliminados = $('#switchEliminadosSintomas').is(':checked');
            },
            dataSrc: function (json) {
                actualizarDiccionarioHijosSintomas(json.data);
                return json.data;
            }
        },
        columns: [
            {
                data: 'nombre',
                orderable: false,
                render: function (data, type, row) {
                    if (!row.esPadre && row.idPadre) {
                        return `<span class="sint-hijo">- ${data}</span>`;
                    }
                    return `<span class="sint-padre">${data}</span>`;
                }
            },
            {
                data: 'esPadre',
                orderable: false,
                render: function (data) {
                    return data
                        ? `<span class="badge badge-grid-gray">Padre</span>`
                        : `<span class="badge badge-grid-gray">Hijo</span>`;
                }
            },
            { data: 'idPadre', orderable: false, defaultContent: '' },
            { data: 'idIdioma', orderable: false },
            // NUEVO: ValidadoIA
            {
                data: 'validadoIA',
                orderable: false,
                render: function (data) {
                    return data 
                        ? '<i class="bi bi-check-circle-fill text-success"></i>' 
                        : '<i class="bi bi-dash-circle text-muted"></i>';
                }
            },
            // NUEVO: ValidadoHumano
            {
                data: 'validadoHumano',
                orderable: false,
                render: function (data) {
                    return data 
                        ? '<i class="bi bi-check-circle-fill text-primary"></i>' 
                        : '<i class="bi bi-dash-circle text-muted"></i>';
                }
            },
            // NUEVO: RelacionEII
            {
                data: 'relacionEII',
                orderable: false,
                render: function (data) {
                    return data 
                        ? '<span class="badge bg-success">Sí</span>' 
                        : '<span class="badge bg-secondary">No</span>';
                }
            },
            {
                data: 'eliminado',
                orderable: false,
                render: function (data) {
                    return data
                        ? `<span class="badge badge-grid-gray">Sí</span>`
                        : `<span class="badge badge-grid-gray">No</span>`;
                }
            },
            {
                data: null,
                orderable: false,
                render: function (data, type, row) {
                    let editBtn = `<button class='btn btn-sm btn-action-sintoma btn-editar-sintoma' data-id='${row.id}'><i class="bi bi-pencil-square"></i> Editar</button>`;
                    let eliminarBtn = '', restaurarBtn = '';
                    
                    const esPadre = row.esPadre;
                    const tieneHijos = hijosSintPorPadre[row.id] && hijosSintPorPadre[row.id].length > 0;
                    
                    if (row.eliminado) {
                        restaurarBtn = `<button class='btn btn-sm btn-success btn-restaurar-sintoma ms-1' data-id='${row.id}'><i class="bi bi-arrow-counterclockwise"></i> Restaurar</button>`;
                    } else {
                        if (!(esPadre && tieneHijos)) {
                            eliminarBtn = `<button class='btn btn-sm btn-danger btn-borrar-sintoma ms-1' data-id='${row.id}'><i class="bi bi-trash"></i> Eliminar</button>`;
                        }
                    }
                    return editBtn + eliminarBtn + restaurarBtn;
                }
            }
        ],
        order: [],
        language: {
            emptyTable: "No hay síntomas registrados."
        }
    });

    // Event: cambiar switch de eliminados
    $('#switchEliminadosSintomas').change(function () {
        sintomasTabla.ajax.reload();
    });

    // Event: abrir panel de edición
    $('#sintomasGrid').on('click', '.btn-editar-sintoma', function () {
        const id = $(this).data('id');
        abrirPanel(id);
    });

    // Event: cerrar panel
    $('#btnCerrarPanel, #btnCancelar').click(function () {
        cerrarPanel();
    });

    // Event: generar descripción IA
    $('#btnGenerarIA').click(async function () {
        const id = sintomaActualId;
        if (!id) {
            alert('No hay síntoma seleccionado');
            return;
        }

        const btn = $(this);
        const originalHTML = btn.html();
        btn.prop('disabled', true).html('<span class="loading-spinner"></span> Generando...');

        try {
            const response = await fetch(`/api/admin/sintomas/${id}/generate-ia-description`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' }
            });

            const data = await response.json();

            if (data.ok) {
                $('#editSintoma_descripcionIA').val(data.descripcion);
                $('#editSintoma_relacionEII').val(
                    data.relacionEII 
                        ? '✅ Sí, documentada' 
                        : '❌ No documentada'
                );
                alert('¡Descripción generada exitosamente!');
            } else {
                alert('Error: ' + (data.error || 'No se pudo generar la descripción'));
            }
        } catch (error) {
            console.error(error);
            alert('Error al conectar con la IA: ' + error.message);
        } finally {
            btn.prop('disabled', false).html(originalHTML);
        }
    });

    // Event: guardar cambios
    $('#formEditarSintoma').on('submit', async function (e) {
        e.preventDefault();
        
        const id = sintomaActualId;
        const formData = {
            nombre: $('#editSintoma_nombre').val(),
            idPadre: $('#editSintoma_idPadre').val() || null,
            idIdioma: parseInt($('#editSintoma_idIdioma').val()),
            icono: $('#editSintoma_icono').val() || '',
            eliminado: $('#editSintoma_eliminado').val() === 'true',
            descripcionIA: $('#editSintoma_descripcionIA').val() || '',
            validadoHumano: $('#editSintoma_validadoHumano').is(':checked')
        };

        try {
            const response = await fetch(`/api/admin/sintomas/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            });

            const data = await response.json();

            if (data.ok) {
                $('#msgEditSintomaSuccess').removeClass('d-none');
                sintomasTabla.ajax.reload(null, false);
                setTimeout(() => {
                    cerrarPanel();
                }, 1500);
            } else {
                alert('Error: ' + (data.error || 'No se pudo guardar.'));
            }
        } catch (error) {
            console.error(error);
            alert('Error al guardar: ' + error.message);
        }
    });

    // Event: eliminar
    $('#sintomasGrid').on('click', '.btn-borrar-sintoma', function () {
        const id = $(this).data('id');
        if (confirm('¿Seguro que desea eliminar este síntoma?')) {
            $.ajax({
                url: '@Url.Page(null, "EliminarSintoma")',
                type: "POST",
                data: $.param({ id: id }),
                contentType: "application/x-www-form-urlencoded; charset=UTF-8",
                success: function (response) {
                    if (response.success) {
                        sintomasTabla.ajax.reload();
                    } else {
                        alert('Error: ' + (response.message || 'No se pudo eliminar.'));
                    }
                }
            });
        }
    });

    // Event: restaurar
    $('#sintomasGrid').on('click', '.btn-restaurar-sintoma', function () {
        const id = $(this).data('id');
        if (confirm('¿Seguro que desea restaurar este síntoma?')) {
            $.ajax({
                url: '@Url.Page(null, "RestaurarSintoma")',
                type: "POST",
                data: $.param({ id: id }),
                contentType: "application/x-www-form-urlencoded; charset=UTF-8",
                success: function (response) {
                    if (response.success) {
                        sintomasTabla.ajax.reload();
                    } else {
                        alert('Error: ' + (response.message || 'No se pudo restaurar.'));
                    }
                }
            });
        }
    });
});
</script>
```

### B. Repetir para Tratamientos

Los mismos cambios deben aplicarse a `Areas/Identity/Pages/Admin/Tratamientos/Index.cshtml`, reemplazando:
- `sintomas` → `tratamientos`
- `sintomasTabla` → `tratamientosTabla`
- URLs `/api/admin/sintomas/` → `/api/admin/tratamientos/`

---

## ✅ VERIFICACIÓN FINAL

1. **Compilar**: `dotnet build`
2. **Ejecutar**: `dotnet run` o F5 en Visual Studio
3. **Probar**:
   - Ir a `/Identity/Admin/Sintomas/Index`
   - Click en "Editar" → debe abrir panel lateral
   - Click en "Generar con IA" → debe llamar a Claude y llenar descripción
   - Guardar cambios
   - Verificar que el grid muestra las columnas nuevas

---

## 📚 ARCHIVOS CREADOS

1. ✅ `Services/AI/ISintomasTratamientosAiService.cs`
2. ✅ `Services/AI/SintomasTratamientosAiService.cs`
3. ✅ `Controllers/SintomasAdminController.cs`
4. ✅ `Controllers/TratamientosAdminController.cs`
5. ✅ `Migrations/SQL_UPDATE_SINTOMAS_TRATAMIENTOS_IA.sql`

---

## 🎯 PRÓXIMOS PASOS OPCIONALES

- Agregar paginación en las notas (SintomasNotas/TratamientosNotas)
- Agregar interfaz para visualizar/editar notas
- Agregar validación de campos en el formulario
- Agregar animaciones al abrir/cerrar panel lateral

---

**¿Necesitas ayuda con algún paso específico?** 🚀
