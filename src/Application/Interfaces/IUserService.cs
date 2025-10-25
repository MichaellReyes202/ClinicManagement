// Define la interfaz para operaciones sobre usuarios
using Application.DTOs;
using Application.DTOs.User;
using Domain.Entities;
using Domain.Errors;

namespace Application.Interfaces
{
    public interface IUserService
    {
        // metodod para obtener el usuario que esta logueado
        Task<User?> GetCurrentUserAsync();

        // obtener todos los usuarios
        Task<Result<PaginatedResponseDto<UserListDto>>> GetAllUsersAsync(PaginationDto pagination);
        Task<string?> GetEmailUserOnlyAsync();
    }
}
