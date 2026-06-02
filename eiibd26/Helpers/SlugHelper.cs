// eiibd26/Helpers/SlugHelper.cs
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;

namespace eiibd26.Helpers
{
    public static class SlugHelper
    {
        /// <summary>
        /// Genera un slug SEO-friendly desde un texto (quita acentos, espacios, caracteres especiales)
        /// </summary>
        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "pregunta";

            // Normalizar (quitar acentos)
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            // Convertir a minúsculas
            var slug = stringBuilder.ToString().Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Trim();

            // Reemplazar espacios y caracteres no válidos por guiones
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            // Limitar longitud a 80 caracteres
            if (slug.Length > 80)
                slug = slug.Substring(0, 80).TrimEnd('-');

            return string.IsNullOrWhiteSpace(slug) ? "pregunta" : slug;
        }

        /// <summary>
        /// Genera un slug único para una pregunta (agrega sufijo numérico si ya existe)
        /// </summary>
        public static async Task<string> GenerateUniqueSlugForPregunta(ApplicationDbContext db, string titulo, Guid? preguntaId = null)
        {
            var baseSlug = GenerateSlug(titulo);
            var slug = baseSlug;
            var counter = 1;

            while (true)
            {
                var exists = await db.Preguntas
                    .AsNoTracking()
                    .AnyAsync(p => p.Slug == slug && (!preguntaId.HasValue || p.Id != preguntaId.Value));

                if (!exists) break;

                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        /// <summary>
        /// Genera un slug único para un perfil médico (agrega sufijo numérico si ya existe).
        /// Deduplicación contra MedicoPerfilExtendido.Slug.
        /// </summary>
        public static async Task<string> GenerateUniqueSlugForMedico(ApplicationDbContext db, string nombre, int? perfilExtId = null)
        {
            var baseSlug = GenerateSlug(nombre);
            if (string.IsNullOrWhiteSpace(baseSlug) || baseSlug == "pregunta")
                baseSlug = "medico";

            var slug = baseSlug;
            var counter = 1;

            while (true)
            {
                var exists = await db.MedicosPerfilExtendido
                    .AsNoTracking()
                    .AnyAsync(p => p.Slug == slug && (!perfilExtId.HasValue || p.Id != perfilExtId.Value));

                if (!exists) break;

                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        /// <summary>
        /// Genera un slug único para un perfil de usuario (deduplicación contra Perfil.slug).
        /// </summary>
        public static async Task<string> GenerateUniqueSlugForUsuario(ApplicationDbContext db, string nombre, Guid? idUser = null)
        {
            var baseSlug = GenerateSlug(nombre);
            if (string.IsNullOrWhiteSpace(baseSlug) || baseSlug == "pregunta") baseSlug = "autor";
            var slug = baseSlug; var counter = 1;
            while (true)
            {
                var exists = await db.Perfil.AsNoTracking()
                    .AnyAsync(p => p.slug == slug && (!idUser.HasValue || p.idUser != idUser.Value));
                if (!exists) break;
                slug = $"{baseSlug}-{counter}"; counter++;
            }
            return slug;
        }
    }
}