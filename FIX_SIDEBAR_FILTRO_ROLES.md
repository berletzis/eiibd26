# ✅ FIX: Filtro de Menú Lateral por Rol

## 🐛 Problema Reportado

Después de un `git pull`, se perdió el filtro que mostraba el menú lateral según el rol del usuario. Específicamente:

- El menú de **Administrador** debe mostrarse **solo** a usuarios con rol "Administrador"
- Si un usuario tiene ambos roles (Administrador + Paciente), debe ver **solo** el menú de Administrador

---

## ✅ Solución Implementada

### Cambio en `eiibd26/Areas/Identity/Pages/Shared/_SidebarMenu.cshtml`:

```razor
@* SECCIÓN ADMIN - Sin cambios (ya estaba correcta) *@
@if (User.Identity?.IsAuthenticated == true && User.IsInRole("Administrador"))
{
    <!-- Menú de Administrador -->
}

@* SECCIÓN PACIENTE - ACTUALIZADA *@
@if (User.Identity?.IsAuthenticated == true && User.IsInRole("Paciente") && !User.IsInRole("Administrador"))
{
    <!-- Menú de Paciente -->
}
```

**Cambio:** Agregado `&& !User.IsInRole("Administrador")` en la condición de Paciente.

---

## 📋 Estructura del Menú

### Menú de Administrador (líneas ~149-217)

```
Admin
├── Contenidos (submenu colapsable)
│   ├── Contenidos
│   ├── Categorías de Contenido
│   └── Banners Inicio
├── Usuarios
├── Condiciones
├── Síntomas
└── Tratamientos
```

### Menú de Paciente (líneas ~222-294)

```
Mi Perfil Público / Configurar Perfil
Panel de Control
Mis P&R
Mi Salud (submenu)
├── Estado de Ánimo
├── Mis condiciones
├── Mis síntomas
├── Seguimiento de síntomas
└── Mis tratamientos
```

---

## 🎯 Lógica de Visualización

| Roles del Usuario | Menú Visible |
|-------------------|--------------|
| **Administrador** | ✅ Solo Admin |
| **Paciente** | ✅ Solo Paciente |
| **Administrador + Paciente** | ✅ Solo Admin (prioridad) |
| **Sin roles** | ❌ Ninguno |
| **Otros roles** | ❌ Ninguno |

**Regla:** Si el usuario es **Administrador**, el menú de Paciente **NO se muestra** aunque también tenga ese rol.

---

## 🔐 Roles en Base de Datos

### AspNetRoles:

```sql
-- Administrador
Id: A9615DE9-B5A6-4DF4-89A8-C444ABF38ADB
Name: Administrador
NormalizedName: ADMINISTRADOR

-- Paciente
Id: D898C186-51A3-4631-90A1-E479C092FEBE
Name: Paciente
NormalizedName: PACIENTE
```

### AspNetUserRoles (relación):
```
UserId (Guid) → FK a AspNetUsers
RoleId (Guid) → FK a AspNetRoles
```

---

## 🧪 Casos de Prueba

### Caso 1: Usuario con rol "Administrador"

**Query SQL:**
```sql
SELECT u.Email, r.Name
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Administrador';
```

**Resultado Esperado:**
- ✅ Ve sección "Admin"
- ✅ Ve Contenidos, Usuarios, Condiciones, Síntomas, Tratamientos
- ❌ NO ve "Mi Perfil Público"
- ❌ NO ve "Panel de Control"
- ❌ NO ve "Mi Salud"

---

### Caso 2: Usuario con rol "Paciente"

**Query SQL:**
```sql
SELECT u.Email, r.Name
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Paciente'
  AND u.Id NOT IN (
      SELECT UserId FROM AspNetUserRoles 
      WHERE RoleId = 'A9615DE9-B5A6-4DF4-89A8-C444ABF38ADB'
  );
```

**Resultado Esperado:**
- ❌ NO ve sección "Admin"
- ✅ Ve "Mi Perfil Público" / "Configurar Perfil"
- ✅ Ve "Panel de Control"
- ✅ Ve "Mis P&R"
- ✅ Ve "Mi Salud" (Estado de Ánimo, Mis condiciones, etc.)

---

### Caso 3: Usuario con AMBOS roles (Administrador + Paciente)

**Query SQL:**
```sql
SELECT u.Email, STRING_AGG(r.Name, ', ') AS Roles
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Id IN (
    SELECT UserId 
    FROM AspNetUserRoles 
    WHERE RoleId IN (
        'A9615DE9-B5A6-4DF4-89A8-C444ABF38ADB',  -- Administrador
        'D898C186-51A3-4631-90A1-E479C092FEBE'   -- Paciente
    )
    GROUP BY UserId
    HAVING COUNT(*) = 2
)
GROUP BY u.Email;
```

**Resultado Esperado:**
- ✅ Ve sección "Admin" (prioridad)
- ✅ Ve Contenidos, Usuarios, Condiciones, Síntomas, Tratamientos
- ❌ NO ve sección de Paciente (bloqueado por `!User.IsInRole("Administrador")`)

---

## 🚀 Pruebas en Frontend

### 1. Verificar en Browser:

```
1. Login como Administrador
2. Ve a cualquier página del área de Identity
3. Verifica que el sidebar muestra solo opciones de Admin
```

### 2. Verificar en DevTools:

```
1. Abre F12 → Elements
2. Busca: .sidebar-menu
3. Verifica que NO existen elementos con clase "salud" o texto "Mi Salud"
```

### 3. Cambiar de Usuario:

```
1. Logout
2. Login como Paciente (sin rol Administrador)
3. Verifica que el sidebar muestra solo opciones de Paciente
4. Verifica que NO existe sección "Admin"
```

---

## 📊 Comparativa Antes/Después

### ANTES del Fix:

| Usuario | Roles | Menú Admin | Menú Paciente |
|---------|-------|------------|---------------|
| Juan | Administrador | ✅ Visible | ❌ No visible |
| María | Paciente | ❌ No visible | ✅ Visible |
| Pedro | Admin + Paciente | ✅ Visible | ⚠️ **TAMBIÉN visible** |

**Problema:** Pedro con ambos roles veía AMBOS menús (confuso).

### DESPUÉS del Fix:

| Usuario | Roles | Menú Admin | Menú Paciente |
|---------|-------|------------|---------------|
| Juan | Administrador | ✅ Visible | ❌ No visible |
| María | Paciente | ❌ No visible | ✅ Visible |
| Pedro | Admin + Paciente | ✅ Visible | ✅ **NO visible** |

**Solución:** Pedro con ambos roles ve solo el menú de Administrador (prioridad).

---

## 🔄 Cambios en Código

### Archivo: `_SidebarMenu.cshtml`

**Línea ~222 - ANTES:**
```razor
@if (User.Identity?.IsAuthenticated == true && User.IsInRole("Paciente"))
```

**Línea ~222 - DESPUÉS:**
```razor
@if (User.Identity?.IsAuthenticated == true && User.IsInRole("Paciente") && !User.IsInRole("Administrador"))
```

**Impacto:** Solo una línea modificada.

---

## 📝 Notas Importantes

1. **Prioridad:** Administrador > Paciente
2. **Sin degradación:** Usuarios con solo un rol no se ven afectados
3. **No afecta seguridad:** Los controladores ya tienen `[Authorize(Roles = "...")]`
4. **Solo UI:** Este cambio es de presentación, no de autorización

---

## ✅ Estado Final

| Aspecto | Estado |
|---------|--------|
| 🟢 Código | ✅ Actualizado |
| 🟢 Build | ✅ Exitoso |
| 🟡 Testing | ⏳ Pendiente verificación |

---

## 🚀 Próximos Pasos

1. **Reiniciar la aplicación:**
   ```
   Shift+F5 (detener)
   F5 (iniciar)
   ```

2. **Probar con diferentes usuarios:**
   - Usuario solo Administrador
   - Usuario solo Paciente
   - Usuario con ambos roles

3. **Verificar visualmente:**
   - Sidebar no debe mostrar secciones no autorizadas
   - Iconos y estilos deben verse correctamente

---

**Estado:** ✅ Implementado y listo para pruebas
