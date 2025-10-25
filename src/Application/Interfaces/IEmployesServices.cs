using Application.DTOs;
using Application.DTOs.Employee;
using Domain.Entities;
using Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEmployesServices
    {
        Task<Result<EmployeReponseDto>> AddEmployesAsync(EmployesCreationDto specialtiesDto);
        Task<Result<PaginatedResponseDto<EmployeeSearchDto>>> EmployeesWithoutUsers(PaginationDto pagination);
        Task<Result<PaginatedResponseDto<EmployeeListDTO>>> GetAllEmployes(PaginationDto pagination);
        Task<Result<EmployeReponseDto>> GetEmployeeById(int Id);
        Task<Result> UpdateEmployesAsync(EmployesUpdateDto dto, int employeeId);
    }
}
