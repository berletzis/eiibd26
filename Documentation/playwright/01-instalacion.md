# Instalación Playwright + MCP — EIIBD

## Fecha
2026-05-26

## Entorno

| Tool | Versión |
|---|---|
| Node.js | v22.22.3 |
| npm | 10.9.8 |
| @playwright/test | instalado en devDependencies |
| @playwright/mcp | instalado globalmente |

## Navegadores instalados

- Chromium 148 → `%LOCALAPPDATA%\ms-playwright\chromium-1223`
- Firefox 150 → `%LOCALAPPDATA%\ms-playwright\firefox-1522`
- WebKit 26.4 → `%LOCALAPPDATA%\ms-playwright\webkit-2287`

## Archivos creados

```
eiibd26/
├── playwright.config.ts        ← config principal (baseURL: https://localhost:7002)
├── .mcp.json                   ← registro MCP para Claude Code
└── tests/playwright/
    ├── login.spec.ts
    ├── registro.spec.ts
    ├── directorio.spec.ts
    └── misalud.spec.ts
```

## MCP Config activo

Archivo: `.mcp.json` en raíz del proyecto.

```json
{
  "mcpServers": {
    "playwright": {
      "command": "playwright-mcp",
      "args": ["--ignore-https-errors"]
    }
  }
}
```

`--ignore-https-errors` es necesario porque EIIBD usa certificado self-signed en desarrollo.

## Activación en Claude Code

Reiniciar Claude Code después de crear `.mcp.json`.
El MCP `playwright` aparecerá disponible automáticamente.
