using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Application.DTOs.specialty;

namespace Application.Services;


public class SpecialtiesServices : ISpecialtiesServices
{
    private readonly ISpecialtiesRepository _specialtiesRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<SpecialtiesUpdateDto> _validatorUpdate;
    private readonly IValidator<SpecialtiesCreateDto> _validatorCreate;


    public SpecialtiesServices
    (
        ISpecialtiesRepository specialtiesRepository, 
        IMapper mapper, 
        IValidator<SpecialtiesUpdateDto> validatorUpdate,
        IValidator<SpecialtiesCreateDto> validatorCreate
    )
    {
        _specialtiesRepository = specialtiesRepository;
        _mapper = mapper;
        _validatorUpdate = validatorUpdate;
        _validatorCreate = validatorCreate;
    }

    public async Task<List<OptionDto>> GetAllSpecialtiesOptions()
    {
        var positions = await _specialtiesRepository.GetAllAsync(act => act.IsActive == true);
        var options =  positions.Select(p => new OptionDto
        {
            Id = p.Id,
            Name = p.Name,
        }).ToList();
        return options;
    }
    public async Task<Result<PaginatedResponseDto<SpecialtyListDto>>> GetAllSpecialties(PaginationDto pagination)
    {
        try
        {
            var (baseQuery, total) = await _specialtiesRepository.GetQueryAndTotal();
            var projectedQuery = baseQuery
                    .Select(e => new SpecialtyListDto
                    {
                        Id = e.Id,
                        Name = e.Name,
                        IsActive = e.IsActive,
                        Description = e.Description,
                        Employees = e.Employees.Count
                    });
            var items = await projectedQuery
                    .Skip(pagination.Offset)
                    .Take(pagination.Limit)
                    .ToListAsync();
            var paginatedResponse = new PaginatedResponseDto<SpecialtyListDto>(total, items);
            return Result<PaginatedResponseDto<SpecialtyListDto>>.Success(paginatedResponse);
        }
        catch (Exception ex)
        {
            return Result<PaginatedResponseDto<SpecialtyListDto>>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }
    public async Task<Result<Specialty>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<Specialty>.Failure(new Error(ErrorCodes.BadRequest, "The id must be a positive integer."));
            }
            var specialty = await _specialtiesRepository.GetByIdAsync(id);
            if (specialty is null)
            {
                return Result<Specialty>.Failure(new Error(ErrorCodes.NotFound, $"The Specialty with the id {id} was not found"));
            }
            return Result<Specialty>.Success(specialty);
        }
        catch (Exception ex)
        {
            return Result<Specialty>.Failure(
                new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
        }
    }
    public async Task<Result<Specialty>> AddSpecialtyAsync(SpecialtiesCreateDto specialtiesDto)
    {
        var validationResult = await _validatorCreate.ValidateAsync(specialtiesDto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result<Specialty>.Failure(errors);
        }
        try
        {
            var existingSpecialty = await _specialtiesRepository.GetByNameAsync(specialtiesDto.Name);
            if (existingSpecialty != null)
            {
                return Result<Specialty>.Failure(
                    new Error(ErrorCodes.Conflict, $"The specialty with the name '{specialtiesDto.Name}' already exists."));
            }
            var specialty = _mapper.Map<Specialty>(specialtiesDto);
            await _specialtiesRepository.AddAsync(specialty);
            await _specialtiesRepository.SaveChangesAsync();
            return Result<Specialty>.Success(specialty);
        }
        catch (Exception ex)
        {
            return Result<Specialty>.Failure(
                new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }


    

    public async Task<Result<Specialty>> UpdateSpecialtyAsync(int id, SpecialtiesUpdateDto specialtiesDto)
    {
        var validationResult = await _validatorUpdate.ValidateAsync(specialtiesDto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result<Specialty>.Failure(errors);
        }
        var specialty = await _specialtiesRepository.GetByIdAsync(id);
        if (specialty == null)
        {
            return Result<Specialty>.Failure(new Error(ErrorCodes.NotFound, "Specialty not found"));
        }
        var existingSpecialty = await _specialtiesRepository.GetByNameAsync(specialtiesDto.Name);
        if (existingSpecialty != null && existingSpecialty.Id != id)
        {
            return Result<Specialty>.Failure(new Error(ErrorCodes.Conflict, $"A specialty with the name '{specialtiesDto.Name}' already exists."));
        }
        _mapper.Map(specialtiesDto, specialty);
        specialty.UpdatedAt = DateTime.UtcNow; // Establecer UpdatedAt en UTC
        try
        {
            await _specialtiesRepository.UpdateAsync(specialty);
            await _specialtiesRepository.SaveChangesAsync();
            return Result<Specialty>.Success(specialty);
        }
        catch (Exception ex)
        {
            return Result<Specialty>.Failure(
                new Error(ErrorCodes.Unexpected, $"An unexpected error occurred: {ex.Message}"));
        }
    }


}
