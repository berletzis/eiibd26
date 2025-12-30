using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using eiibd26.Data;

namespace eiibd26.Pages.Home
{
    public class UsersMapPartialModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public UsersMapPartialModel(ApplicationDbContext db) { _db = db; }

        // Países para el combobox
        public List<(string Code, string Name)> Countries { get; set; } = new();

        // JSON con perfiles (para inyectar en la vista)
        public string ProfilesJson { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            // Cargar países
            try
            {
                var paises = await _db.Paises
                    .AsNoTracking()
                    .Where(p => !p.Borrado && p.VIsibleBuscador == true)
                    .OrderBy(p => p.PaisNombre)
                    .Select(p => new { Code = p.PaisCodigo, Name = p.PaisNombre })
                    .ToListAsync();

                Countries = paises.Select(x => (x.Code ?? "", x.Name ?? "")).ToList();
            }
            catch
            {
                Countries = new List<(string, string)>();
            }

            // Cargar perfiles con lat/lng (Perfil.Latitud / Perfil.Longitud en tu modelo)
            var raw = await _db.Perfil
                .AsNoTracking()
                .Where(p => !string.IsNullOrWhiteSpace(p.Latitud) && !string.IsNullOrWhiteSpace(p.Longitud))
                .Select(p => new
                {
                    p.idUser,
                    Name = p.Nombre,
                    Country = p.NombrePais,
                    CountryCode = p.NombrePais, // si tienes un código distinto, ajusta aquí
                    Latitude = p.Latitud,
                    Longitude = p.Longitud
                })
                .ToListAsync();

            var list = new List<object>();
            foreach (var r in raw)
            {
                var latStr = (r.Latitude ?? "").Trim().Replace(',', '.');
                var lngStr = (r.Longitude ?? "").Trim().Replace(',', '.');

                if (double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                 && double.TryParse(lngStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                {
                    list.Add(new
                    {
                        id = r.idUser.ToString(),
                        name = string.IsNullOrWhiteSpace(r.Name) ? "Usuario" : r.Name,
                        country = r.Country ?? "",
                        countryCode = (r.CountryCode ?? "").ToUpperInvariant(),
                        lat,
                        lng
                    });
                }
            }

            ProfilesJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }
}