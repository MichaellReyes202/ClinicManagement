using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Role;
using FluentValidation;

namespace Application.Validators.Role
{
    public class RoleCreactionDtoValidator : AbstractValidator<RoleDto>
    {
        public RoleCreactionDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Role name is required.")
                .MinimumLength(4).WithMessage("Role name must be at least 4 characters long.")
                // Validar que el nombre del rol no tenga numeros o caracteres especiales
                .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Role description is required.")
                .MaximumLength(200).WithMessage("Role description must not exceed 200 characters.");
        }
    }
}
