using Application.DTOs;
using Application.DTOs.ExamType;
using Application.DTOs.Patient;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Services;
public class ExamTypeServices : IExamTypeServices
{
    private readonly IExamTypeRepository _examTypeRepository;
    private readonly ISpecialtiesRepository _specialtiesRepository;
    private readonly IUserService _userService;
    private readonly IValidator<ExamTypeCreateDto> _validator;
    private readonly IValidator<ExamTypeUpdateDto> _validatorUpdate;
    private readonly IMapper _mapper;

    public ExamTypeServices
    (
        IExamTypeRepository examTypeRepository , 
        ISpecialtiesRepository specialtiesRepository,
        IUserService userService ,
        IValidator<ExamTypeCreateDto> validator ,
        IValidator<ExamTypeUpdateDto> validatorUpdate,

        IMapper mapper
    )
    {
        _examTypeRepository = examTypeRepository;
        _specialtiesRepository = specialtiesRepository;
        _userService = userService;
        _validator = validator;
        _validatorUpdate = validatorUpdate;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedResponseDto<ExamTypeListDto>>> GetAll(PaginationDto pagination)
    {
        try
        {
            var searchTerm = pagination.Query?.Trim().ToLower();
            Expression<Func<ExamType, bool>> searchFilter = null;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchFilter = e => e.Name.Contains(searchTerm);
            }
            var (baseQuery, total) = await _examTypeRepository.GetQueryAndTotal( filter: searchFilter , include : q => q.Include(e => e.Specialty) );
            var projectedQuery = baseQuery
                .AsNoTracking()
                .Select(e => new ExamTypeListDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    DeliveryTime = e.DeliveryTime,
                    IsActive = e.IsActive,
                    PricePaid = e.PricePaid,
                    SpecialtyId = e.SpecialtyId,
                    SpecialtyName = e.Specialty.Name
                });
            var items = await projectedQuery
                .Skip(pagination.Offset)
                .Take(pagination.Limit)
                .ToListAsync();

            var paginatedResponse = new PaginatedResponseDto<ExamTypeListDto>(total, items);
            return Result<PaginatedResponseDto<ExamTypeListDto>>.Success(paginatedResponse);
        }
        catch (Exception ex)
        {
            return Result<PaginatedResponseDto<ExamTypeListDto>>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }

    public async Task<Result<ExamTypeResponseDto>> GetById(int Id)
    {
        try
        {
            if (Id <= 0) return Result<ExamTypeResponseDto>.Failure(new Error(ErrorCodes.BadRequest, $"The ID : {Id} sent is not valid.", nameof(Id)));
            var examType = await _examTypeRepository.GetByIdAsync(Id);
            if (examType == null)
            {
                return Result<ExamTypeResponseDto>.Failure(new Error(ErrorCodes.NotFound, $"ExamType {Id} not found", nameof(Id)));
            }
            var patientDto = _mapper.Map<ExamTypeResponseDto>(examType);
            return Result<ExamTypeResponseDto>.Success(patientDto);
        }
        catch (Exception ex)
        {
            return Result<ExamTypeResponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }
    //public async Task<Result<List<DoctorBySpecialtyDto>>> GetDoctorBySpecialty()
    //{

    //}
    public async Task<Result<ExamTypeResponseDto>> Add(ExamTypeCreateDto examTypeDto)
    {
        var valitationResult = await _validator.ValidateAsync(examTypeDto);
        if (!valitationResult.IsValid)
        {
            var errors = valitationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result<ExamTypeResponseDto>.Failure(errors);
        }
        try
        {
            using var transacion = await _examTypeRepository.BeginTransactionAsync();
            try
            {
                // obtener el usuario de la sesion
                var userOnly = await _userService.GetCurrentUserAsync();

                // Verificar que el nombre del examen no este registrado 
                var existExamType = await _examTypeRepository.ExistAsync(x => x.Name.ToUpper() == examTypeDto.Name.ToUpper());
                if (existExamType)
                {
                    return Result<ExamTypeResponseDto>.Failure(new Error(ErrorCodes.Conflict, $"The name of the specialty already exists.", "name"));
                }

                // Verificar el id de la especidad que sea valido 
                var existeSpeciality = await _specialtiesRepository.ExistAsync(x => x.Id == examTypeDto.SpecialtyId);
                if (!existeSpeciality)
                {
                    return Result<ExamTypeResponseDto>.Failure(new Error(ErrorCodes.NotFound, $"The specialty  id : {examTypeDto.SpecialtyId} was not found", "specialtyId"));
                }
                var examtype = _mapper.Map<ExamType>(examTypeDto);
                examtype.CreatedAt = DateTime.UtcNow;
                examtype.CreatedByUserId = userOnly?.Id;

                await _examTypeRepository.AddAsync(examtype);
                await _examTypeRepository.SaveChangesAsync();
                await transacion.CommitAsync();

                var examDto = _mapper.Map<ExamTypeResponseDto>(examtype);
                return Result<ExamTypeResponseDto>.Success(examDto);

            }
            catch (DbUpdateException ex)
            {
                await transacion.RollbackAsync();
                return Result<ExamTypeResponseDto>.Failure(new Error(ErrorCodes.Conflict, "A unique data conflict has occurred. Please try again."));
            }

        }
        catch (Exception ex)
        {
            return Result<ExamTypeResponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }



    public async Task<Result> Update(ExamTypeUpdateDto examTypeDto , int examTypeId)
    {
        var valitationResult = await _validatorUpdate.ValidateAsync(examTypeDto);
        if (!valitationResult.IsValid)
        {
            var errors = valitationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result<ExamTypeResponseDto>.Failure(errors);
        }
        try
        {
            using var transacion = await _examTypeRepository.BeginTransactionAsync();
            try
            {
                // obtener el usuario de la sesion
                var userOnly = await _userService.GetCurrentUserAsync();

                // verificar que el examen exista 
                var examtype = await _examTypeRepository.GetByIdAsync(examTypeId);
                if (examtype is null)
                    return Result.Failure(new Error(ErrorCodes.NotFound, $"Employee with ID '{examTypeId}' does not exist."));

                // verificar que el id del dto no sea diferente a id del parametro
                if (examTypeDto.Id != examTypeId)
                    return Result.Failure(new Error(ErrorCodes.BadRequest, $"The ExamType {examTypeId} in the route does not match the ID sent in the request body {examTypeDto.Id}"));

                // Verificar que el nombre del examen no este registrado 
                var existExamType = await _examTypeRepository.ExistAsync(x => x.Name.ToUpper() == examTypeDto.Name.ToUpper() && x.Id != examTypeId);
                if (existExamType)
                {
                    return Result.Failure(new Error(ErrorCodes.Conflict, $"The name of the specialty already exists.", "name"));
                }

                // Verificar el id de la especidad que sea valido 
                var existeSpeciality = await _specialtiesRepository.ExistAsync(x => x.Id == examTypeDto.SpecialtyId);
                if (!existeSpeciality)
                {
                    return Result.Failure(new Error(ErrorCodes.NotFound, $"The specialty  id : {examTypeDto.SpecialtyId} was not found", "specialtyId"));
                }
                examtype.UpdatedAt = DateTime.UtcNow;
                examtype.UpdatedByUserId = userOnly?.Id;

                _mapper.Map(examTypeDto, examtype);

                await _examTypeRepository.Update(examtype);
                await _examTypeRepository.SaveChangesAsync();
                await transacion.CommitAsync();
                return Result.Success();

            }
            catch (DbUpdateException ex)
            {
                await transacion.RollbackAsync();
                return Result<ExamTypeResponseDto>.Failure(new Error(ErrorCodes.Conflict, "A unique data conflict has occurred. Please try again."));
            }

        }
        catch (Exception ex)
        {
            return Result<ExamTypeResponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }
}
