
using System.ComponentModel.DataAnnotations;

// Contiene la información para registrar un nuevo usuario
namespace Application.DTOs
{
    public class RegisterDto 
    {
        public int EmployeeId { get; set; }
        public int RoleId { get; set; }
    }
}
