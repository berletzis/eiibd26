namespace eiibd26.Models.Campanas
{
    /// <summary>
    /// Criterio de público objetivo para una campaña de email.
    /// Cada valor define el WHERE que se aplica sobre AspNetUsers — todos traducibles a SQL por EF Core.
    /// </summary>
    public enum PublicoCampana
    {
        /// <summary>
        /// Usuarios migrados del sistema antiguo.
        /// SQL: LEN(PasswordHash) = 68 AND EmailConfirmed = 1
        /// Longitud 68 chars base64 corresponde al formato de hash del sistema anterior (Identity v2 / custom).
        /// </summary>
        UsuariosViejos = 1,

        /// <summary>
        /// Usuarios nativos registrados en el nuevo sistema (o ya reactivados con nueva contraseña).
        /// SQL: PasswordHash LIKE 'AQAAAA%' AND EmailConfirmed = 1
        /// Prefijo AQAAAA = byte marcador 0x01 de Identity v3 (PBKDF2/SHA-256), longitud 84 chars base64.
        /// </summary>
        UsuariosNuevos = 2,

        /// <summary>
        /// Todos los usuarios con email confirmado, sin distinción de formato de hash.
        /// SQL: EmailConfirmed = 1
        /// </summary>
        TodosConfirmados = 3
    }
}
