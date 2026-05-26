# Verificación Playwright MCP — EIIBD

## Pre-requisitos

1. EIIBD corriendo en `https://localhost:7002`
2. Claude Code reiniciado (para cargar el MCP)

## Verificar instalación CLI

```bash
playwright-mcp --help
npx playwright --version
```

## Test rápido de conectividad

```bash
npx playwright test tests/playwright/login.spec.ts --headed
```

## Verificar MCP activo en Claude Code

En una conversación nueva, pedir:

> Usa Playwright para abrir https://localhost:7002 y tomar un screenshot

Si el MCP está activo, Claude Code podrá navegar sin comandos manuales.

## Problemas comunes

| Problema | Causa | Solución |
|---|---|---|
| ERR_CERT_AUTHORITY_INVALID | Cert self-signed | `--ignore-https-errors` en `.mcp.json` |
| MCP no aparece | Claude Code no reiniciado | Cerrar y reabrir Claude Code |
| Timeout navegando | App no levantada | Ejecutar `dotnet run` primero |
