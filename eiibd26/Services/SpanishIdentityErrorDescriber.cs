using Microsoft.AspNetCore.Identity;

namespace eiibd26.Services
{
    /// <summary>
    /// Traduce todos los mensajes de error de ASP.NET Identity al español
    /// </summary>
    public class SpanishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() 
            => new IdentityError { Code = nameof(DefaultError), Description = "Ha ocurrido un error desconocido." };

        public override IdentityError ConcurrencyFailure() 
            => new IdentityError { Code = nameof(ConcurrencyFailure), Description = "Error de concurrencia, el objeto ha sido modificado." };

        public override IdentityError PasswordMismatch() 
            => new IdentityError { Code = nameof(PasswordMismatch), Description = "Contraseña incorrecta." };

        public override IdentityError InvalidToken() 
            => new IdentityError { Code = nameof(InvalidToken), Description = "El token es inválido." };

        public override IdentityError LoginAlreadyAssociated() 
            => new IdentityError { Code = nameof(LoginAlreadyAssociated), Description = "Ya existe un usuario con este inicio de sesión." };

        public override IdentityError InvalidUserName(string userName) 
            => new IdentityError { Code = nameof(InvalidUserName), Description = $"El nombre de usuario '{userName}' es inválido. Solo puede contener letras o dígitos." };

        public override IdentityError InvalidEmail(string email) 
            => new IdentityError { Code = nameof(InvalidEmail), Description = $"El email '{email}' es inválido." };

        public override IdentityError DuplicateUserName(string userName) 
            => new IdentityError { Code = nameof(DuplicateUserName), Description = $"El nombre de usuario '{userName}' ya está en uso." };

        public override IdentityError DuplicateEmail(string email) 
            => new IdentityError { Code = nameof(DuplicateEmail), Description = $"El email '{email}' ya está en uso." };

        public override IdentityError InvalidRoleName(string role) 
            => new IdentityError { Code = nameof(InvalidRoleName), Description = $"El rol '{role}' es inválido." };

        public override IdentityError DuplicateRoleName(string role) 
            => new IdentityError { Code = nameof(DuplicateRoleName), Description = $"El rol '{role}' ya está en uso." };

        public override IdentityError UserAlreadyHasPassword() 
            => new IdentityError { Code = nameof(UserAlreadyHasPassword), Description = "El usuario ya tiene una contraseña establecida." };

        public override IdentityError UserLockoutNotEnabled() 
            => new IdentityError { Code = nameof(UserLockoutNotEnabled), Description = "El bloqueo no está habilitado para este usuario." };

        public override IdentityError UserAlreadyInRole(string role) 
            => new IdentityError { Code = nameof(UserAlreadyInRole), Description = $"El usuario ya tiene el rol '{role}'." };

        public override IdentityError UserNotInRole(string role) 
            => new IdentityError { Code = nameof(UserNotInRole), Description = $"El usuario no tiene el rol '{role}'." };

        // ⚠️ MENSAJES DE CONTRASEÑA (LOS MÁS IMPORTANTES)
        public override IdentityError PasswordTooShort(int length) 
            => new IdentityError { Code = nameof(PasswordTooShort), Description = $"La contraseña debe tener al menos {length} caracteres." };

        public override IdentityError PasswordRequiresNonAlphanumeric() 
            => new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "La contraseña debe contener al menos un carácter especial (!@#$%^&*)." };

        public override IdentityError PasswordRequiresDigit() 
            => new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "La contraseña debe contener al menos un número (0-9)." };

        public override IdentityError PasswordRequiresLower() 
            => new IdentityError { Code = nameof(PasswordRequiresLower), Description = "La contraseña debe contener al menos una letra minúscula (a-z)." };

        public override IdentityError PasswordRequiresUpper() 
            => new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "La contraseña debe contener al menos una letra mayúscula (A-Z)." };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) 
            => new IdentityError { Code = nameof(PasswordRequiresUniqueChars), Description = $"La contraseña debe contener al menos {uniqueChars} caracteres únicos." };

        public override IdentityError RecoveryCodeRedemptionFailed() 
            => new IdentityError { Code = nameof(RecoveryCodeRedemptionFailed), Description = "Falló la recuperación del código." };
    }
}
