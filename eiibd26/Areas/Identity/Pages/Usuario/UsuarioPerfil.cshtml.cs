using eiibd26.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    [IgnoreAntiforgeryToken]
    public class UsuarioPerfilModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public UsuarioPerfilModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public Perfil Perfil { get; set; }

        [BindProperty]
        public bool EditMode { get; set; } = false;

        [TempData]
        public string SuccessMessage { get; set; }

        // Lista pública de países para el combo
        public List<Paises> PaisesLista { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id = null)
        {
            if (id == null)
            {
                var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null || !Guid.TryParse(userId, out Guid guidId))
                    return NotFound();
                id = guidId;
            }

            Perfil = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == id);

            // Pobla la lista de países para el select
            PaisesLista = await _db.Paises
                .Where(p => !p.Borrado && p.VIsibleBuscador)
                .OrderBy(p => p.PaisNombre)
                .ToListAsync();

            if (Perfil == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Repopula la lista de países para errores en el POST
            PaisesLista = await _db.Paises
                .Where(p => !p.Borrado && p.VIsibleBuscador)
                .OrderBy(p => p.PaisNombre)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                SuccessMessage = null;
                return Page();
            }

            var dbPerfil = await _db.Perfil.FirstOrDefaultAsync(p => p.idUser == Perfil.idUser);
            if (dbPerfil == null) return NotFound();

            dbPerfil.Avatar = Perfil.Avatar;
            dbPerfil.imagenFondo = Perfil.imagenFondo;
            dbPerfil.Titulo = Perfil.Titulo;
            dbPerfil.Activo = Perfil.Activo;
            dbPerfil.Nombre = Perfil.Nombre;
            dbPerfil.Apellidos = Perfil.Apellidos;
            dbPerfil.Telefono = Perfil.Telefono;
            dbPerfil.Email = Perfil.Email;
            dbPerfil.FechaDeNacimiento = Perfil.FechaDeNacimiento;
            dbPerfil.UsoPlataforma = Perfil.UsoPlataforma;
            dbPerfil.idZone = Perfil.idZone;
            //dbPerfil.notas = Perfil.notas;
            //dbPerfil.descripcion = Perfil.descripcion;
            dbPerfil.slug = Perfil.slug;
            dbPerfil.Genero = Perfil.Genero;
            dbPerfil.Latitud = Perfil.Latitud;
            dbPerfil.Longitud = Perfil.Longitud;
            dbPerfil.NombreCiudad = Perfil.NombreCiudad;
            dbPerfil.NombrePais = Perfil.NombrePais;
            dbPerfil.AceptoPP = Perfil.AceptoPP;
            dbPerfil.UltimosEstudios = Perfil.UltimosEstudios;
            dbPerfil.ExperienciaLaboral = Perfil.ExperienciaLaboral;
            dbPerfil.UltimaCertificacion = Perfil.UltimaCertificacion;
            dbPerfil.AcercaDe = Perfil.AcercaDe;
            dbPerfil.Extras = Perfil.Extras;
            dbPerfil.FechaModificado = DateTime.Now;

            _db.Perfil.Update(dbPerfil);
            await _db.SaveChangesAsync();

            SuccessMessage = "Los datos del perfil se han actualizado correctamente.";
            return RedirectToPage(new { id = Perfil.idUser });
        }
    }
}