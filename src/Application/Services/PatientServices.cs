using Application.DTOs;
using Application.DTOs.Employee;
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
public class PatientServices : IPatientServices
{
    private readonly IPatientRepository _patientRepository;
    private readonly IValidator<PatientCreateDto> _createValidator;
    private readonly IValidator<PatientUpdateDto> _updateValidator;
    private readonly IUserService _userService;
    private readonly ICatalogServices _catalogServices;
    private readonly IMapper _mapper;

    public PatientServices(
        IPatientRepository patientRepository , 
        IValidator<PatientCreateDto> createValidator , 
        IValidator<PatientUpdateDto> updateValidator,
        IUserService userService , 
        ICatalogServices catalogServices ,
        IMapper mapper
    )
    {
        _patientRepository = patientRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _userService = userService;
        _catalogServices = catalogServices;

        _mapper = mapper;
    }

    public async Task<Result<PaginatedResponseDto<PatientSearchDto>>> SearchPatient(PaginationDto pagination)
    {
        var searchTerm = pagination.Query?.Trim().ToLower();
        Expression<Func<Patient, bool>> searchFilter = null;

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchFilter = e =>
                (e.FirstName.ToLower().Contains(searchTerm) || // Solo el primer nombre
                (e.MiddleName != null && e.MiddleName.ToLower().Contains(searchTerm)) || // Si existe, el segundo nombre
                 e.LastName.ToLower().Contains(searchTerm) || // Apellido
                 (e.SecondLastName != null && e.SecondLastName.ToLower().Contains(searchTerm)) ||
                 (e.Dni != null && e.Dni.ToLower().Contains(searchTerm)));
        }
        var (baseQuery, total) = await _patientRepository.GetQueryAndTotal( filter: searchFilter);
        var projectQuery = baseQuery
            .AsNoTracking()
            .Select(e => new PatientSearchDto
            {
                Id = e.Id,
                FullName = (e.FirstName + " " + (e.MiddleName != null ? e.MiddleName + " " : "") + e.LastName + " " + (e.SecondLastName != null ? e.SecondLastName : "")).Trim(),
                Dni = e.Dni ?? string.Empty  ,
                ContactPhone = e.ContactPhone ?? string.Empty,
            });
        var items = await projectQuery
            .Skip(pagination.Offset)
            .Take(pagination.Limit)
            .ToListAsync();

        var paginatedResponse = new PaginatedResponseDto<PatientSearchDto>(total, items);
        return Result<PaginatedResponseDto<PatientSearchDto>>.Success(paginatedResponse);
    }

    public async Task<Result<PaginatedResponseDto<PatientResponseDto>>> GetAllPatients(PaginationDto pagination)
    {
        try
        {
            var searchTerm = pagination.Query?.Trim().ToLower();
            Expression<Func<Patient, bool>> searchFilter = null;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchFilter = e =>
                    (e.FirstName.ToLower().Contains(searchTerm) || // Solo el primer nombre
                    (e.MiddleName != null && e.MiddleName.ToLower().Contains(searchTerm)) || // Si existe, el segundo nombre
                     e.LastName.ToLower().Contains(searchTerm) || // Apellido
                     (e.SecondLastName != null && e.SecondLastName.ToLower().Contains(searchTerm)) || 
                     (e.Dni != null && e.Dni.ToLower().Contains(searchTerm)));
            }
            var (baseQuery, total) = await _patientRepository.GetQueryAndTotal(include: q => q.Include(e => e.PatientGuardian) , filter: searchFilter);
            var projectedQuery = baseQuery
                .AsNoTracking()
                .Select(e => new PatientResponseDto
                {
                    Id = e.Id,
                    FirstName = e.FirstName,
                    MiddleName = e.MiddleName,
                    LastName = e.LastName,
                    SecondLastName = e.SecondLastName,
                    Age = DateTime.Now.Year - e.DateOfBirth.Year - (DateTime.Now.DayOfYear < e.DateOfBirth.DayOfYear ? 1 : 0),
                    DateOfBirth = e.DateOfBirth,
                    Dni = e.Dni,
                    ContactPhone = e.ContactPhone,
                    ContactEmail = e.ContactEmail,
                    Address = e.Address,
                    SexId = e.SexId,
                    BloodTypeId = e.BloodTypeId,
                    ConsultationReasons = e.ConsultationReasons,
                    ChronicDiseases = e.ChronicDiseases,
                    Allergies = e.Allergies,
                    CreatedAt = e.CreatedAt,
                    Guardian = e.PatientGuardian != null ? new PatientGuardianDto
                    {
                        FullName = e.PatientGuardian.FullName,
                        Relationship = e.PatientGuardian.Relationship,
                        ContactPhone = e.PatientGuardian.ContactPhone,
                        Dni = e.PatientGuardian.Dni
                    } : null
                });
            var items = await projectedQuery
                .Skip(pagination.Offset)
                .Take(pagination.Limit)
                .ToListAsync();

            var paginatedResponse = new PaginatedResponseDto<PatientResponseDto>(total, items);
            return Result<PaginatedResponseDto<PatientResponseDto>>.Success(paginatedResponse);
        }
        catch (Exception ex)
        {
            return Result<PaginatedResponseDto<PatientResponseDto>>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }

    public async Task<Result<PatientResponseDto>> GetPatientById(int Id)
    {
        try
        {
            if (Id <= 0) return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.BadRequest, $"The ID : {Id} sent is not valid.", nameof(Id)));

            var query = await _patientRepository.GetQuery(filter: c => c.Id == Id,include: q => q.Include(e => e.PatientGuardian));
            var patient =  await query.AsNoTracking().FirstOrDefaultAsync();
            if (patient == null)
            {
                return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.NotFound, $"Employee {Id} not found", nameof(Id)));
            }
            var patientDto = _mapper.Map<PatientResponseDto>(patient);
            return Result<PatientResponseDto>.Success(patientDto);
        }
        catch (Exception ex)
        {
            return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }

    public async Task<Result<PatientResponseDto>> AddPatientAsync(PatientCreateDto patient)
    {
        var valitationResult = await _createValidator.ValidateAsync(patient);
        if (!valitationResult.IsValid)
        {
            var errors = valitationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result<PatientResponseDto>.Failure(errors);
        }
        try
        {
            using var transacion = await _patientRepository.BeginTransactionAsync();
            try
            {
                // obtener el usuario de la sesion
                var userOnly = await _userService.GetCurrentUserAsync();

                // Verificar que el dni no este registrado en otro usuario (primer verificar si se envio el dni)
                if ( !string.IsNullOrEmpty(patient.Dni))
                {
                    var existDni = await _patientRepository.ExistAsync(c => c.Dni.ToUpper() == patient.Dni.ToUpper());
                    if (existDni)
                        return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.Conflict, $"The ID number '{patient.Dni}' already exists.", "dni"));
                }
                // Verificar que el email no este registrado en otro usuario
                if (!string.IsNullOrEmpty(patient.ContactEmail))
                {
                    var existEmail = await _patientRepository.ExistAsync(c => c.ContactEmail.ToUpper() == patient.ContactEmail.ToUpper());
                    if (existEmail)
                        return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.Conflict, $"The email '{patient.ContactEmail}' already exists.", "contactEmail"));
                }

                // Verificar el contactPhone
                if (!string.IsNullOrEmpty(patient.ContactPhone))
                {
                    var existPhone = await _patientRepository.ExistAsync(c => c.ContactPhone.ToUpper() == patient.ContactPhone.ToUpper());
                    if (existPhone)
                        return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.Conflict, $"The phone number '{patient.ContactPhone}' already exists.", "contactPhone"));
                }

                // verificar que el id del sexo exista
                var existSex = await _catalogServices.ExistSexId(patient.SexId);
                if (!existSex )
                {
                    return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.NotFound, $"The sex id : {patient.SexId} was not found", "SexId"));
                }
                // Verificar que el id del tipo de sangre exista
                if( patient.BloodTypeId.HasValue )
                {
                    var existBlood = await _catalogServices.ExistBloodId(patient.BloodTypeId.Value);
                    if (!existBlood)
                    {
                        return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.NotFound, $"The blood type id : {patient.BloodTypeId} was not found", "BloodTypeId"));
                    }
                }
                var paciente = _mapper.Map<Patient>(patient);

                if (patient.Guardian != null)
                {
                    var guardian = _mapper.Map<PatientGuardian>(patient.Guardian);
                    paciente.PatientGuardian = guardian;
                }
                paciente.CreatedByUserId = userOnly?.Id;
                await _patientRepository.AddAsync(paciente);
                await _patientRepository.SaveChangesAsync();
                await transacion.CommitAsync();

                var patientDto = _mapper.Map<PatientResponseDto>(paciente);
                return Result<PatientResponseDto>.Success(patientDto);

            }
            catch (DbUpdateException ex)
            {
                await transacion.RollbackAsync();
                return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.Conflict, "A unique data conflict has occurred. Please try again."));
            }

        }
        catch (Exception ex)
        {
            return Result<PatientResponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }

    public async Task<Result> UpdatePatientAsync(PatientUpdateDto dto  , int patientId)
    {
        var valitationResult = await _updateValidator.ValidateAsync(dto);
        if (!valitationResult.IsValid)
        {
            var errors = valitationResult.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList();
            return Result<PatientResponseDto>.Failure(errors);
        }
        try
        {
            using var transacion = await _patientRepository.BeginTransactionAsync();
            try
            {
                // obtener el usuario de la sesion
                var userOnly = await _userService.GetCurrentUserAsync();

                // buscar al paciente por su id 
                var query = await _patientRepository.GetQuery(filter: c => c.Id == patientId, include: q => q.Include(e => e.PatientGuardian));
                var patient = await query.FirstOrDefaultAsync();
                if (patient is null)
                    return Result.Failure(new Error(ErrorCodes.NotFound, $"Patient with ID '{patientId}' does not exist."));

                // verificar que el id del dto no sea diferente a id del parametro
                if (dto.Id != patientId)
                    return Result.Failure(new Error(ErrorCodes.BadRequest, $"The patient {patientId} in the route does not match the ID sent in the request body {dto.Id}"));

                // Verificar que el dni no este registrado en otro usuario (primer verificar si se envio el dni)
                if (!string.IsNullOrEmpty(patient.Dni))
                {
                    var existDni = await _patientRepository.ExistAsync(c => c.Dni.ToUpper() == patient.Dni.ToUpper() && c.Id != patientId);
                    if (existDni)
                        return Result.Failure(new Error(ErrorCodes.Conflict, $"The ID number '{patient.Dni}' already exists.", "dni"));
                }
                // Verificar que el email no este registrado en otro usuario
                if (!string.IsNullOrEmpty(patient.ContactEmail))
                {
                    var existEmail = await _patientRepository.ExistAsync(c => c.ContactEmail.ToUpper() == patient.ContactEmail.ToUpper() && c.Id != patientId);
                    if (existEmail)
                        return Result.Failure(new Error(ErrorCodes.Conflict, $"The email '{patient.ContactEmail}' already exists.", "contactEmail"));
                }

                // Verificar el contactPhone
                if (!string.IsNullOrEmpty(patient.ContactPhone))
                {
                    var existPhone = await _patientRepository.ExistAsync(c => c.ContactPhone.ToUpper() == patient.ContactPhone.ToUpper() && c.Id != patientId);
                    if (existPhone)
                        return Result.Failure(new Error(ErrorCodes.Conflict, $"The phone number '{patient.ContactPhone}' already exists.", "contactPhone"));
                }

                // verificar que el id del sexo exista
                var existSex = await _catalogServices.ExistSexId(patient.SexId);
                if (!existSex)
                {
                    return Result.Failure(new Error(ErrorCodes.NotFound, $"The sex id : {patient.SexId} was not found", "SexId"));
                }
                // Verificar que el id del tipo de sangre exista
                if (patient.BloodTypeId.HasValue)
                {
                    var existBlood = await _catalogServices.ExistBloodId(patient.BloodTypeId.Value);
                    if (!existBlood)
                    {
                        return Result.Failure(new Error(ErrorCodes.NotFound, $"The blood type id : {patient.BloodTypeId} was not found", "BloodTypeId"));
                    }
                }
                // mapear los cambios del dto al paciente

                _mapper.Map(dto, patient);

                if (patient.PatientGuardian != null)
                {
                    var guardian = _mapper.Map<PatientGuardian>(dto.Guardian);
                    patient.PatientGuardian = guardian;
                }
                patient.UpdatedByUserId = userOnly?.Id;
                
                await _patientRepository.UpdatePatientAsync(patient);
                await _patientRepository.SaveChangesAsync();
                await transacion.CommitAsync();

                return Result.Success();

            }
            catch (DbUpdateException ex)
            {
                await transacion.RollbackAsync();
                return Result.Failure(new Error(ErrorCodes.Conflict, "A unique data conflict has occurred. Please try again."));
            }

        }
        catch (Exception ex)
        {
            return Result.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
        }
    }
}
