using Application.DTOs;
using Application.DTOs.Role;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RoleServices : IRoleService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService;
        private readonly RoleManager<Role> _roleManager;
        private readonly IValidator<RoleDto> _validator_Role;
        private readonly IRoleRepository _roleRepository;
        private readonly IMapper _mapper;

        public RoleServices
        (
            UserManager<User> userManager , 
            IUserService userService , 
            RoleManager<Role> roleManager ,
            IValidator<RoleDto> validator_role ,
            IRoleRepository roleRepository ,
            IMapper mapper
        )
        {
            _userManager = userManager;
            _userService = userService;
            _roleManager = roleManager;
            _validator_Role = validator_role;
            _roleRepository = roleRepository;
            _mapper = mapper;
        }

        public async Task<List<OptionDto>> GetAllRolesOptions()
        {
            var roles = await _roleRepository.GetAllAsync();
            var options = roles.Select(p => new OptionDto
            {
                Id = p.Id,
                Name = p.Name,
            }).ToList();
            return options;
        }
        public async Task<Result<Role>> GetRoleByIdAsync(int id)
        {
            var role = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id);
            if (role is null)
            {
                Result<Role>.Failure(new Error(ErrorCodes.NotFound, $"Role with id {id} was not found"));
            }
            return Result<Role>.Success(role!);
        }
        public async Task<Result> AssignRoleToUserAsync(AssignRoleDto assignRoleDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(assignRoleDto.Email);
                if (user is null)
                {
                    return Result.Failure(new Error(ErrorCodes.Unauthorized, $"user with email {assignRoleDto.Email} was not found","Email"));
                }
                var roleExists = await _roleManager.RoleExistsAsync(assignRoleDto.RoleName);
                if (!roleExists)
                {
                    return Result.Failure(new Error(ErrorCodes.NotFound, $"Role {assignRoleDto.RoleName} does not exist", "RoleName"));
                }
                var isInRole = await _userManager.IsInRoleAsync(user, assignRoleDto.RoleName);
                if (isInRole)
                {
                    return Result.Failure(new Error(ErrorCodes.Conflict, $"User {assignRoleDto.Email} already has the role {assignRoleDto.RoleName}", "RoleName"));
                }
                try
                {
                    var result = await _userManager.AddToRoleAsync(user, assignRoleDto.RoleName);

                    if (!result.Succeeded)
                    {
                        var errors = result.Errors
                            .Select(e => new ValidationError(string.Empty, e.Description))
                            .ToList();
                        return Result.Failure(errors);
                    }
                    return Result.Success();
                }
                catch(DbUpdateException  dbEx)
                {
                    return Result.Failure(new Error(ErrorCodes.Unexpected, $"Database error: {dbEx.Message}"));
                }

            }
            catch (Exception ex)
            {
                return Result.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
            }
            
        }

        public async Task<Result<Role>> CreateRoleAsync(RoleDto roleCreaction)
        {
            var validationResult = _validator_Role.Validate(roleCreaction);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                    .ToList();
                return Result<Role>.Failure(errors);
            }
            try
            {
                await _roleRepository.BeginTransactionAsync();
                try
                {
                    
                    var existingRole = await _roleRepository.FindByNameAsync(roleCreaction.Name);
                    if (existingRole != null)
                    {
                        return Result<Role>.Failure(new Error(ErrorCodes.Conflict, $"Role with name {roleCreaction.Name} already exists", "Name"));
                    }
                    var role = _mapper.Map<Role>(roleCreaction);

                    var result = await _roleManager.CreateAsync(role);
                    await _roleRepository.CommitAsync();
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors
                            .Select(e => new ValidationError(string.Empty, e.Description))
                            .ToList();
                        return Result<Role>.Failure(errors);
                    }

                    return Result<Role>.Success(role);
                }
                catch (DbUpdateException dbEx)
                {
                    return Result<Role>.Failure(new Error(ErrorCodes.Unexpected, $"Database error: {dbEx.Message}"));
                }
            }
            catch (Exception ex)
            {
                await _roleRepository.RollbackAsync();
                return Result<Role>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
            }            
        }


        public async Task<Result> UpdateRoleAsync(int idRole , RoleDto roleDto)
        {
            var validationResult = _validator_Role.Validate(roleDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                    .ToList();
                return Result.Failure(errors);
            }

            // buscar el rol por el id 
            var role = _roleManager.Roles.FirstOrDefault(r => r.Id == idRole);
            if(role is null)
            {
                return Result.Failure(new Error(ErrorCodes.NotFound, $"Role with id {idRole} was not found"));
            }

            role.Name = roleDto.Name;
            role.Description = roleDto.Description;
            role.UpdatedAt = DateTime.UtcNow;
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(e => new ValidationError(string.Empty, e.Description))
                    .ToList();
                return Result.Failure(errors);
            }
            return Result.Success();
        }

        public Task DeleteRoleAsync(string roleName)
        {
            throw new NotImplementedException();
        }

        public string GetRoleName(string roleName)
        {
            throw new NotImplementedException();
        }

        public Task<IList<string>> GetUserRolesAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetUsersInRoleAsync(string roleName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsUserInRoleAsync(string email, string roleName)
        {
            throw new NotImplementedException();
        }

        public Task RemoveRoleFromUserAsync(string email, string roleName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RoleExistsAsync(string roleName)
        {
            throw new NotImplementedException();
        }

        
    }
}
