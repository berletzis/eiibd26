# ===== INSTALAR PAQUETES NUGET NECESARIOS =====

# 1. Hangfire para background jobs
dotnet add package Hangfire.Core --version 1.8.12
dotnet add package Hangfire.SqlServer --version 1.8.12
dotnet add package Hangfire.AspNetCore --version 1.8.12

# ===== VERIFICACIÓN =====
# Ejecutar para verificar que se instalaron:
dotnet list package

# ===== NOTA =====
# System.Net.Http (HttpClient) ya está incluido en .NET 8/10
# System.Text.Json ya está incluido en .NET 8/10
# No necesitas instalar paquetes adicionales para Anthropic Claude
