using System.Threading.Tasks;

namespace eiibd26.Services.Analytics
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardDto> GetDashboardAsync();
    }

    public class AdminDashboardDto
    {
        // Usuarios
        public int TotalUsuarios { get; set; }
        public int UsuariosConfirmados { get; set; }
        public int NuevosEstaSemana { get; set; }
        public int Activos30d { get; set; }
        public int Activos90d { get; set; }
        public int PerfilesCompletos { get; set; }
        public int PerfilesBasicos { get; set; }
        public int PerfilesMinimos { get; set; }

        // Comunidad
        public int TotalPreguntas { get; set; }
        public int PreguntasEstaSemana { get; set; }
        public int PreguntasSinResponder { get; set; }
        public int TotalRespuestasHumanas { get; set; }
        public int TotalRespuestasNINA { get; set; }
        public int TotalVotos { get; set; }

        // Salud
        public int RegistrosMoodEstaSemana { get; set; }
        public int TotalUsuariosConMood { get; set; }
        public int TotalUsuariosConCondicion { get; set; }
        public int TotalTrackingSintomas { get; set; }

        // Contenidos
        public int TotalPublicados { get; set; }
        public int TotalBorradores { get; set; }
        public double PromedioCalidadGris { get; set; }

        // Médicos
        public int TotalDirectorio { get; set; }
        public int MedicosVerificados { get; set; }
        public int MedicosReclamados { get; set; }
        public int TotalConfirmacionesComunitarias { get; set; }

        // Marketing
        public int TotalShortUrls { get; set; }
        public long TotalClicksShortUrls { get; set; }
        public int ClicksEstaSemana { get; set; }
        public int TotalPushEnviadas { get; set; }
        public int CampañasEmailEnviadas { get; set; }
    }
}
