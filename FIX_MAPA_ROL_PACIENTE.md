# ✅ FIX: Filtro de Mapa por Rol "Paciente"

## 🐛 Problema Original

En la página `/Mapa/Index`, la lógica de filtrado por rol "Paciente" era **demasiado restrictiva**:

```csharp
// ❌ ANTES (líneas 138-139):
_db.UserRoles.Count(ur => ur.UserId == p.idUser) == 1 &&  // Solo usuarios con EXACTAMENTE 1 rol
_db.UserRoles.Any(ur => ur.UserId == p.idUser && ur.RoleId == pacienteRoleId)
```

**Problema:** Excluía a usuarios que tuvieran **más de un rol** (ej: Paciente + Administrador, Paciente + Moderador).

---

## ✅ Solución Implementada

### Cambios en `eiibd26/Pages/Mapa/Index.cshtml.cs` (líneas 127-154):

```csharp
// ✅ DESPUÉS:
// 1. Obtener usuarios con rol Paciente
var usersWithPacienteRole = _db.UserRoles
    .Where(ur => ur.RoleId == pacienteRoleId)
    .Select(ur => ur.UserId);

// 2. Filtrar perfiles de esos usuarios
var basePerfil = _db.Perfil.AsNoTracking()
    .Where(p =>
        !string.IsNullOrWhiteSpace(p.Latitud) &&
        !string.IsNullOrWhiteSpace(p.Longitud) &&
        usersWithPacienteRole.Contains(p.idUser)  // ✅ Cualquier usuario con rol Paciente
    );
```

### Mejoras:

1. ✅ **Incluye TODOS los usuarios con rol "Paciente"**, independientemente de si tienen otros roles
2. ✅ **Manejo de error**: Si no existe el rol "Paciente", devuelve lista vacía
3. ✅ **Mejor rendimiento**: Un solo query a `AspNetUserRoles` en lugar de dos subconsultas por cada perfil

---

## 📋 Contexto: Estructura de Roles

### Tablas Involucradas:

```sql
-- AspNetRoles
Id: D898C186-51A3-4631-90A1-E479C092FEBE
Name: Paciente
NormalizedName: PACIENTE

-- AspNetUserRoles (relación many-to-many)
UserId: Guid (FK a AspNetUsers)
RoleId: Guid (FK a AspNetRoles)

-- Perfil (datos de ubicación del usuario)
idUser: Guid (FK a AspNetUsers)
Latitud: string
Longitud: string
```

---

## 🎯 Lógica de Filtrado

### ANTES:
```
Usuario tiene EXACTAMENTE 1 rol
  Y ese rol es "Paciente"
    → Incluir en mapa
```
**Problema:** Usuario con 2+ roles → ❌ Excluido

### DESPUÉS:
```
Usuario tiene el rol "Paciente"
  (puede tener otros roles también)
    → Incluir en mapa
```
**Resultado:** Usuario con rol Paciente + otros roles → ✅ Incluido

---

## 🧪 Casos de Prueba

### Escenarios de Usuario:

| Usuario | Roles | ANTES | DESPUÉS |
|---------|-------|-------|---------|
| Juan | Paciente | ✅ Incluido | ✅ Incluido |
| María | Paciente, Administrador | ❌ **Excluido** | ✅ **Incluido** |
| Pedro | Paciente, Moderador | ❌ **Excluido** | ✅ **Incluido** |
| Ana | Administrador | ❌ Excluido | ❌ Excluido |
| Luis | (sin rol) | ❌ Excluido | ❌ Excluido |

---

## 🚀 Pruebas

### 1. Verificar en SQL:

```sql
-- Ver usuarios con rol Paciente
SELECT 
    u.Id,
    u.Email,
    u.UserName,
    COUNT(ur.RoleId) AS TotalRoles,
    STRING_AGG(r.Name, ', ') AS Roles
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Id IN (
    SELECT UserId 
    FROM AspNetUserRoles 
    WHERE RoleId = 'D898C186-51A3-4631-90A1-E479C092FEBE'
)
GROUP BY u.Id, u.Email, u.UserName
ORDER BY COUNT(ur.RoleId) DESC;
```

### 2. Verificar en Frontend:

```
1. Navega a: https://localhost:7002/mapa
2. Abre DevTools (F12) → Network
3. Filtra por: OnGetProfilesAsync
4. Verifica que la respuesta incluya usuarios con múltiples roles
```

### 3. API Endpoint:

```
GET /Mapa?handler=Profiles&skip=0&take=48
```

**Verificar:** La respuesta debe incluir perfiles de usuarios que tengan el rol "Paciente", incluso si tienen otros roles adicionales.

---

## 📊 Performance

### Comparativa de Queries:

**ANTES:**
```sql
-- Por cada perfil (N queries):
SELECT COUNT(*) FROM AspNetUserRoles WHERE UserId = @userId;  -- Query 1
SELECT 1 FROM AspNetUserRoles WHERE UserId = @userId AND RoleId = @pacienteRoleId;  -- Query 2
```

**DESPUÉS:**
```sql
-- Una sola query inicial:
SELECT UserId FROM AspNetUserRoles WHERE RoleId = @pacienteRoleId;

-- Luego IN clause:
SELECT * FROM Perfil WHERE idUser IN (...)
```

**Resultado:** ✅ Mejor rendimiento con carga de base de datos reducida.

---

## 🔐 Seguridad

### Validación:
- ✅ Solo usuarios con rol "Paciente" aparecen en el mapa
- ✅ Si el rol no existe, devuelve lista vacía (no error)
- ✅ Cache invalidado correctamente con versión

### Cache:
```csharp
var key = $"Mapa:Profiles:v{version}:c={country}|cond={conditionId}|...";
```
- ✅ Cache por 2 minutos para resultados vacíos
- ✅ Cache por 10 minutos para resultados válidos

---

## 📝 Resumen

| Aspecto | Estado |
|---------|--------|
| 🟢 Código | ✅ Corregido |
| 🟢 Build | ✅ Exitoso |
| 🟢 Performance | ✅ Mejorado |
| 🟡 Testing | ⏳ Pendiente verificación |

---

## 🚀 Próximos Pasos

1. **Reiniciar la aplicación:**
   ```
   Shift+F5 (detener)
   F5 (iniciar)
   ```

2. **Probar en el mapa:**
   - Verificar que aparezcan usuarios con rol "Paciente"
   - Verificar que NO aparezcan usuarios sin ese rol
   - Verificar que usuarios con múltiples roles (incluyendo Paciente) SÍ aparezcan

3. **Monitorear logs:**
   ```
   Ver Output → Debug para verificar queries SQL generadas
   ```

---

## ⚠️ Notas Importantes

1. **No afecta otras páginas:** Este cambio solo aplica al mapa de usuarios
2. **Cache:** Puede tomar hasta 10 minutos en reflejarse si hay resultados cacheados
3. **Rol ID:** Hardcoded `D898C186-51A3-4631-90A1-E479C092FEBE` (Paciente)

---

**Estado:** ✅ Implementado y listo para pruebas
