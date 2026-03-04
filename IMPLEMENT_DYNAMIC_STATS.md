# 🚀 IMPLEMENTACIÓN: Cards Dinámicos + Info Paginación Arriba

## 📋 CAMBIOS A REALIZAR:

### **1. Backend: Index.cshtml.cs (línea 382)**

**REEMPLAZAR:**
```csharp
public async Task<IActionResult> OnGetEstadisticasAsync()
```

**POR:**
```csharp
public async Task<IActionResult> OnGetEstadisticasAsync(string filterHash = "", string filterLockout = "", string filterCondicion = "", string filterPais = "")
```

**Y AGREGAR** después de la línea 405 (antes de la proyección):
```csharp
// Base query  
var usersQuery = _userManager.Users.AsQueryable();

// ⭐ APLICAR FILTROS
if (!string.IsNullOrWhiteSpace(filterHash))
{
    bool hashValid = filterHash == "true";
    usersQuery = usersQuery.Where(u => 
        (hashValid && u.PasswordHash != null && u.PasswordHash.Length >= 50 && u.PasswordHash.StartsWith("AQAAAA")) ||
        (!hashValid && (u.PasswordHash == null || u.PasswordHash.Length < 50 || !u.PasswordHash.StartsWith("AQAAAA")))
    );
}

if (!string.IsNullOrWhiteSpace(filterLockout))
{
    bool isLocked = filterLockout == "true";
    if (isLocked)
    {
        usersQuery = usersQuery.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow);
    }
    else
    {
        usersQuery = usersQuery.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTimeOffset.UtcNow);
    }
}

// Filtro por condición (COPIAR del método OnGetGridDataAsync, líneas 84-120)

// Filtro por país
if (!string.IsNullOrWhiteSpace(filterPais))
{
    usersQuery = usersQuery.Where(u => 
        _db.Perfil.Any(p => p.idUser == u.Id && p.NombrePais == filterPais)
    );
}

// MODIFICAR línea 401:
var basePerfil = _db.Perfil.AsNoTracking()
    .Where(p =>
        !string.IsNullOrWhiteSpace(p.Latitud) &&
        !string.IsNullOrWhiteSpace(p.Longitud) &&
        _db.UserRoles.Any(ur => ur.UserId == p.idUser && ur.RoleId == pacienteRoleId) &&
        usersQuery.Any(u => u.Id == p.idUser) // ⭐ NUEVO: Aplicar filtros
    );
```

---

### **2. Vista: Index.cshtml - AGREGAR INFO ARRIBA DEL GRID**

**BUSCAR** (línea ~220):
```html
<div class="table-centered-admin">
```

**AGREGAR ANTES:**
```html
<!-- ⭐ Info de paginación arriba del grid -->
<div class="mb-2 d-flex justify-content-between align-items-center" style="font-size:0.9rem;color:#64748b;">
    <div id="tableInfoTop">Cargando...</div>
    <div class="text-muted" style="font-size:0.85rem;">
        <i class="bi bi-info-circle me-1"></i>Usa los filtros para refinar los resultados
    </div>
</div>
```

---

### **3. JavaScript: ACTUALIZAR loadStats**

**BUSCAR** (línea ~250):
```javascript
function loadStats() {
    $.get('@Url.Page(null, "Estadisticas")', function(data) {
```

**REEMPLAZAR POR:**
```javascript
function loadStats() {
    // ⭐ Enviar filtros actuales
    var params = {
        filterHash: $('#filterHash').val(),
        filterLockout: $('#filterLockout').val(),
        filterCondicion: $('#filterCondicion').val(),
        filterPais: $('#filterPais').val()
    };
    
    $.get('@Url.Page(null, "Estadisticas")', params, function(data) {
        $('#statCompletos').text(data.perfilesCompletos || 0);
        $('#statBasicos').text(data.perfilesBasicos || 0);
        $('#statMinimos').text(data.perfilesMinimos || 0);
    });
}
```

---

### **4. JavaScript: AGREGAR eventos onChange**

**BUSCAR** (línea ~300):
```javascript
$.get('@Url.Page(null, "Paises")', function(data) {
```

**AGREGAR DESPUÉS** del bloque de DataTables:
```javascript
// ⭐ NUEVO: Recargar cards al cambiar filtros
$('#filterHash, #filterLockout, #filterCondicion, #filterPais').on('change', function() {
    loadStats(); // Recargar estadísticas con filtros actuales
});
```

---

### **5. JavaScript: ACTUALIZAR info de paginación**

**BUSCAR** (línea ~370 - configuración de DataTables):
```javascript
var table = $('#usersGrid').DataTable({
```

**AGREGAR** en la configuración (después de `language:`):
```javascript
drawCallback: function(settings) {
    var api = this.api();
    var info = api.page.info();
    
    // Actualizar info arriba del grid
    if (info.recordsDisplay > 0) {
        $('#tableInfoTop').html(
            '<strong>Mostrando ' + (info.start + 1) + ' a ' + info.end + 
            ' de ' + info.recordsDisplay + ' usuarios</strong>' +
            (info.recordsDisplay !== info.recordsTotal ? 
                ' <span style="color:#999;">(filtrado de ' + info.recordsTotal + ' totales)</span>' : '')
        );
    } else {
        $('#tableInfoTop').text('No se encontraron usuarios con los filtros seleccionados');
    }
}
```

---

### **6. JavaScript: ACTUALIZAR botones**

**BUSCAR** "Aplicar Filtros" (línea ~470):
```javascript
$('#btnApplyFilters').on('click', function() {
```

**AGREGAR AL INICIO:**
```javascript
loadStats(); // ⭐ Recargar cards
```

**BUSCAR** "Limpiar" (línea ~477):
```javascript
$('#btnClearFilters').on('click', function() {
```

**AGREGAR AL FINAL:**
```javascript
loadStats(); // ⭐ Recargar cards
```

---

## ✅ RESULTADO ESPERADO:

**SIN FILTROS:**
```
🏆 45 Completos  |  📊 80 Básicos  |  ⚠️ 25 Mínimos
Mostrando 1 a 10 de 150 usuarios
[Grid con 10 filas]
```

**CON FILTRO (Condición=Colitis):**
```
🏆 15 Completos  |  📊 30 Básicos  |  ⚠️ 10 Mínimos  ← ⭐ Actualizados
Mostrando 1 a 10 de 55 usuarios (filtrado de 150 totales)
[Grid con 10 filas de Colitis]
```

**CON FILTRO (Condición=Colitis + País=México):**
```
🏆 8 Completos   |  📊 12 Básicos  |  ⚠️ 3 Mínimos   ← ⭐ Actualizados
Mostrando 1 a 10 de 23 usuarios (filtrado de 150 totales)
[Grid con 10 filas de Colitis en México]
```

---

## 🚨 IMPORTANTE:

**DEBES REINICIAR LA APP** después de hacer estos cambios (Hot Reload NO funciona con cambios en métodos async):

```powershell
Ctrl + C
dotnet run
```

---

¿Necesitas ayuda con algún paso específico?
