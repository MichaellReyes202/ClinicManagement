using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.User;
public class UserListDto
{
    public int Id { get; set; } // id del usuario
    public int? EmployerId { get; set; } // id del empleado 
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public string FullName { get; set; } = null!; 
    public string? Dni { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Roles { get; set; }
}

