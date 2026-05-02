using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    /// <summary>
    /// DTO para que el administrador restablezca la contraseña de un usuario.
    /// Requiere la contraseña del admin para confirmar la acción.
    /// </summary>
    public class AdminResetPasswordDto
    {
        [Required]
        public int TargetUserId { get; set; }

        [Required]
        public string AdminPassword { get; set; } = string.Empty;
    }
}
