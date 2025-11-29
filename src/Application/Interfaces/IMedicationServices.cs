using Application.DTOs;
using Application.DTOs.Medication;
using Domain.Errors;

namespace Application.Interfaces;

public interface IMedicationServices
{
    Task<Result<PaginatedResponseDto<MedicationDto>>> GetAll(PaginationDto pagination);
    Task<Result<MedicationDto>> GetById(int id);
}
