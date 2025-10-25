using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Role
{
    public class AssignRoleDto
    {
        public required string Email { get; set; }
        public required string RoleName { get; set; }
    }
}
