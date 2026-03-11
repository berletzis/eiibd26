using eiibd26.Models.Glossary;
using eiibd26.Services.Glossary;
using eiibd26.Services.Glossary.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages.Glosario
{
    public class TerminoModel : PageModel
    {
        private readonly IGlossaryService _glossaryService;
        private readonly ILogger<TerminoModel> _logger;
        private readonly UserManager<eiibd26.Models.ApplicationUser> _userManager;

        // Roles autorizados para validar
        private static readonly string[] _validationRoles = ["Medico", "Administrador"];

        public TerminoModel(
            IGlossaryService glossaryService,
            ILogger<TerminoModel> logger,
            UserManager<eiibd26.Models.ApplicationUser> userManager)
        {
            _glossaryService = glossaryService;
            _logger = logger;
            _userManager = userManager;
        }

        public GlossaryTermDetailDto? Term { get; set; }

        /// <summary>Indica si el usuario actual puede validar (Médico o Administrador)</summary>
        public bool CanValidate { get; set; }

        [TempData]
        public string? ValidationMessage { get; set; }

        [TempData]
        public bool ValidationSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            try
            {
                Term = await _glossaryService.GetTermBySlugAsync(slug);

                if (Term == null)
                {
                    _logger.LogWarning("Término con slug '{Slug}' no encontrado", slug);
                    return NotFound();
                }

                await LoadValidationPermissionAsync();

                _logger.LogInformation("Término '{Nombre}' cargado exitosamente", Term.Nombre);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar término con slug '{Slug}'", slug);
                return NotFound();
            }
        }

        /// <summary>
        /// POST: registrar validación de descripción o nivel de relación médica.
        /// Solo roles Médico / Administrador.
        /// </summary>
        public async Task<IActionResult> OnPostAddValidationAsync(
            int termId,
            string slug,
            int validationType,
            int? medicalRelationTypeId,
            string? comment)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return Challenge();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => _validationRoles.Contains(r)))
            {
                ValidationMessage = "No tienes permiso para validar términos. Se requiere rol Médico o Administrador.";
                ValidationSuccess = false;
                return RedirectToPage(new { slug });
            }

            if (!Enum.IsDefined(typeof(GlossaryValidationType), validationType))
            {
                ValidationMessage = "Tipo de validación no válido.";
                ValidationSuccess = false;
                return RedirectToPage(new { slug });
            }

            MedicalRelationType? relationType = null;
            if (medicalRelationTypeId.HasValue)
            {
                if (!Enum.IsDefined(typeof(MedicalRelationType), medicalRelationTypeId.Value))
                {
                    ValidationMessage = "Nivel de relación no válido.";
                    ValidationSuccess = false;
                    return RedirectToPage(new { slug });
                }
                relationType = (MedicalRelationType)medicalRelationTypeId.Value;
            }

            var added = await _glossaryService.AddValidationAsync(
                termId,
                user.Id.ToString(),
                (GlossaryValidationType)validationType,
                relationType,
                comment);

            ValidationSuccess = added;
            ValidationMessage = added
                ? "Validación registrada correctamente."
                : "Ya registraste esta validación anteriormente.";

            return RedirectToPage(new { slug });
        }

        private async Task LoadValidationPermissionAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return;

            var roles = await _userManager.GetRolesAsync(user);
            CanValidate = roles.Any(r => _validationRoles.Contains(r));
        }
    }
}
