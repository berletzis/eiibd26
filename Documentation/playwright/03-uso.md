# Uso Playwright MCP en Claude Code — EIIBD

## Capacidades disponibles

Con el MCP activo Claude Code puede:

- Abrir cualquier URL del sitio
- Hacer click en elementos
- Rellenar formularios (login, registro, perfil)
- Tomar screenshots
- Leer el DOM y detectar errores JS
- Evaluar accesibilidad y UX
- Navegar flujos completos sin intervención manual

## Flujos principales

### Login
```
Ir a /Identity/Account/Login
Rellenar email + contraseña
Click "Iniciar sesión"
Verificar redirect a dashboard
Screenshot
```

### Registro paciente
```
Ir a /Identity/Account/Register
Rellenar campos obligatorios
Aceptar condiciones
Submit
Verificar email de confirmación
```

### Registro médico
```
Ir a /Identity/Account/RegisterM
Rellenar datos + número colegiado
Submit
Verificar estado pendiente
```

### Directorio médicos
```
Ir a /directorio
Verificar cards visibles
Click en médico
Verificar página de detalle
```

## Comandos manuales (sin MCP)

```bash
# Ejecutar todos los tests
npx playwright test

# Solo un archivo
npx playwright test tests/playwright/login.spec.ts

# Con navegador visible
npx playwright test --headed

# Solo Chromium
npx playwright test --project=chromium

# Ver reporte HTML
npx playwright show-report
```

## Auditoría UX con Playwright

Pedir a Claude Code:

> Navega por los módulos de Mi Salud como usuario logueado y reporta
> cualquier problema visual, error JS o inconsistencia de UX.

Claude Code usará el MCP para navegar e inspeccionar en tiempo real.
