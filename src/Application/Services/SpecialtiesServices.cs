using Application.DTOs;
using Application.DTOs.ExamType;
using Application.DTOs.Patient;
using Application.DTOs.specialty;
using Application.DTOs.Specialty;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Result<PaginatedResponseDto<ExamsBySpecialtyListDto>>> GetExamsBySpecialty(PaginationDto pagination)
    {
        var (baseQuery, total) = await _specialtiesRepository.GetQueryAndTotal(include: q => q.Include(e => e.ExamTypes));

        var projectedQuery = baseQuery
                .AsNoTracking()
                .Select(e => new ExamsBySpecialtyListDto
                {
                    Id= e.Id,
                    Name= e.Name,
                    Description = e.Description,
                    ExamTypes = e.ExamTypes.Select(p => new ExamTypeListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        DeliveryTime = p.DeliveryTime,
                        IsActive = p.IsActive,
                        PricePaid = p.PricePaid,
                        SpecialtyId = p.SpecialtyId,
                        SpecialtyName = e.Name
                    }).ToList(),
                });
        var items = await projectedQuery
               .Skip(pagination.Offset)
               .Take(pagination.Limit)
               .ToListAsync();

        var paginatedResponse = new PaginatedResponseDto<ExamsBySpecialtyListDto>(total, items);
        return Result<PaginatedResponseDto<ExamsBySpecialtyListDto>>.Success(paginatedResponse);
    }
    
    public async Task< Result< List<DoctorBySpecialtyDto>>> GetDoctorBySpecialty(PaginationDto pagination)
    {
        var baseQuery = await _specialtiesRepository.GetQuery(include: q => q.Include(e => e.Employees));

        var projectedQuery = baseQuery
            .AsNoTracking()
            .Select(e => new DoctorBySpecialtyDto
            {
                Id = e.Id,
                Name = e.Name,
                Doctors = e.Employees
                    .Where(em => em.PositionId == 1) // Position 1 (Cargo del doctor)
                    .Select(emp => new OptionDto
                    {
                        Id = emp.Id,
                        Name = emp.FirstName + " " + (emp.MiddleName ?? "") + " " + emp.LastName + " " + (emp.SecondLastName ?? "")
                    })
                    .ToList()
            });

        var items = await projectedQuery
           .Skip(pagination.Offset)
           .Take(pagination.Limit)
           .ToListAsync();

        return Result<List<DoctorBySpecialtyDto>>.Success(items);
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
