using Application.DTOs;
using Application.DTOs.Medication;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Services;

public class MedicationServices : IMedicationServices
{
    private readonly IMedicationRepository _medicationRepository;
    private readonly IMapper _mapper;

    public MedicationServices(IMedicationRepository medicationRepository, IMapper mapper)
    {
        _medicationRepository = medicationRepository;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedResponseDto<MedicationDto>>> GetAll(PaginationDto pagination)
    {
        try
        {
            var searchTerm = pagination.Query?.Trim().ToLower();
            Expression<Func<Medication, bool>> searchFilter = null;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchFilter = e => e.Name.ToLower().Contains(searchTerm) || 
                                    (e.GenericName != null && e.GenericName.ToLower().Contains(searchTerm));
            }

            var (baseQuery, total) = await _medicationRepository.GetQueryAndTotal(filter: searchFilter);
            
            var items = await baseQuery
                .AsNoTracking()
                .Skip(pagination.Offset)
                .Take(pagination.Limit)
                .Select(e => new MedicationDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    GenericName = e.GenericName,
                    Presentation = e.Presentation,
                    Concentration = e.Concentration,
                    Description = e.Description,
                    Price = e.Price,
                    IsActive = e.IsActive
                })
                .ToListAsync();

            var paginatedResponse = new PaginatedResponseDto<MedicationDto>(total, items, pagination.Limit);
            return Result<PaginatedResponseDto<MedicationDto>>.Success(paginatedResponse);
        }
        catch (Exception ex)
        {
            return Result<PaginatedResponseDto<MedicationDto>>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
        }
    }

    public async Task<Result<MedicationDto>> GetById(int id)
    {
        try
        {
            if (id <= 0) return Result<MedicationDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid ID"));

            var medication = await _medicationRepository.GetByIdAsync(id);
            if (medication == null)
            {
                return Result<MedicationDto>.Failure(new Error(ErrorCodes.NotFound, $"Medication {id} not found"));
            }

            var dto = _mapper.Map<MedicationDto>(medication); // Ensure mapping exists or do manual mapping
            return Result<MedicationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<MedicationDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
        }
    }
}
