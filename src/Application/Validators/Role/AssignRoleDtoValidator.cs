using Application.DTOs.Role;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators.Role
{
    public class AssignRoleDtoValidator : AbstractValidator<AssignRoleDto>
    {
        public AssignRoleDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("The email format is not valid.");
            RuleFor(x => x.RoleName)
                .Matches("^[a-zA-Z]+$").WithMessage("Role name must contain only letters.")
                .NotEmpty().WithMessage("Role name is required.")
                .MinimumLength(4).WithMessage("Role name must be at least 4 characters long.")
                .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");

        }
    }
}
