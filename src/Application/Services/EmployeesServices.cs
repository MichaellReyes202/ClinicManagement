using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;


namespace Application.Services
{
    public class EmployesServices : IEmployesServices
    {
        private readonly IEmployesRepository _employeesRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<EmployesCreationDto> _validator;
        private readonly IValidator<EmployesUpdateDto> _updateValidator;
        private readonly ISpecialtiesRepository _specialtiesRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IUserService _userService;

        public EmployesServices
        (
            IEmployesRepository employeesRepository,
            IUserRepository userRepository,
            IMapper mapper,
            IValidator<EmployesCreationDto> validator,
            IValidator<EmployesUpdateDto> updateValidator,
            ISpecialtiesRepository specialtiesRepository,
            IPositionRepository positionRepository,
            IUserService userService
        )
        {
            _employeesRepository = employeesRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _validator = validator;
            _updateValidator = updateValidator;
            _specialtiesRepository = specialtiesRepository;
            _positionRepository = positionRepository;
            _userService = userService;
        }
        public async Task<Result<PaginatedResponseDto<EmployeeListDTO>>> GetAllEmployes(PaginationDto pagination)
        {
            try
            {
                var (baseQuery, total) = await _employeesRepository.GetQueryAndTotal(include: q => q.Include(e => e.Position).Include(e => e.Specialty));
                var projectedQuery = baseQuery
                    .Select(e => new EmployeeListDTO
                    {
                        Id = e.Id,
                        FullName = $"{e.FirstName} {e.LastName}",
                        Dni = e.Dni,
                        PositionName = e.Position.Name,
                        EspecialtyName = e.Specialty != null ? e.Specialty.Name : "N/A",
                        ContactPhone = e.ContactPhone,
                        Email = e.Email,
                        IsActive = e.IsActive,
                        PositionId = e.PositionId,
                        SpecialtyId = e.SpecialtyId,
                    });
                var items = await projectedQuery
                    .Skip(pagination.Offset)
                    .Take(pagination.Limit)
                    .ToListAsync();

                var paginatedResponse = new PaginatedResponseDto<EmployeeListDTO>(total, items);
                return Result<PaginatedResponseDto<EmployeeListDTO>>.Success(paginatedResponse);
            }
            catch (Exception ex)
            {
                return Result<PaginatedResponseDto<EmployeeListDTO>>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
            }
        }

        // Obtener el empleado por el id  return CreatedAtRoute("ObtenerAutor",new {id = autor.Id}, autorDTO);
        public async Task<Result<EmployeReponseDto>> GetEmployeeById(int Id)
        {
            try
            {
                if (Id <= 0) return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.BadRequest, $"The ID : {Id} sent is not valid.",nameof(Id)));

                var employee = await _employeesRepository.GetByIdAsync(Id);
                if (employee == null)
                {
                    return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.NotFound, $"Employee {Id} not found" , nameof(Id)));
                }
                var employeeDto = _mapper.Map<EmployeReponseDto>(employee);
                return Result<EmployeReponseDto>.Success(employeeDto);
            }
            catch ( Exception ex )
            {
                return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
            }
        }

        // Actualiza empleado 
        public async Task<Result> UpdateEmployesAsync(EmployesUpdateDto dto , int employeeId  )
        {
            var valitationResult = await _updateValidator.ValidateAsync(dto);
            if (!valitationResult.IsValid)
            {
                var errors = valitationResult.Errors
                    .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                    .ToList();
                return Result.Failure(errors);
            }
            try
            {
                using var transacion = await _employeesRepository.BeginTransactionAsync();
                try
                {
                    var userOnly = await _userService.GetCurrentUserAsync();

                    var employe = await _employeesRepository.GetByIdAsync(employeeId);
                    if (employe is null)
                        return Result.Failure(new Error(ErrorCodes.NotFound, $"Employee with ID '{employeeId}' does not exist."));

                    // verificar que el id del dto no sea diferente a id del parametro
                    if (dto.Id != employeeId) return Result.Failure(new Error(ErrorCodes.BadRequest, $"The employee {employeeId} in the route does not match the ID sent in the request body {dto.Id}"));

                    // verificar que el nuevo email enviado otro usuario no lo tenga en uso 
                    var verifyEmail = await _employeesRepository.ExistAsync(e => e.NormalizedEmail == dto.Email.ToUpper() && e.Id != employeeId);
                    if (verifyEmail)
                        return Result.Failure(new Error(ErrorCodes.Conflict, "The email provided is already in use by another employee.", "email"));

                    // verificar que la nueva cedula enviado no este registrada en otro usuarios
                    var verifyDni = await _employeesRepository.ExistAsync(e => e.Dni == dto.Dni.ToUpper() && e.Id != employeeId);
                    if (verifyDni)
                        return Result.Failure(new Error(ErrorCodes.Conflict, "The ID number provided is already in use by another employee.", "dni"));


                    // TODO : Falta validar que la edad concuerde con la cedula y formato del dni 


                    // Verificar que la especialidad existe ( si es que se mando una )
                    if (dto.SpecialtyId.HasValue)
                    {
                        var specialtyExists = await _specialtiesRepository.GetByIdAsync(dto.SpecialtyId.Value) != null;
                        if (!specialtyExists)
                            return Result.Failure(new Error(ErrorCodes.NotFound, $"The specialty with ID '{dto.SpecialtyId.Value}' does not exist."));
                    }
                    var positionExists = await _positionRepository.GetByIdAsync(dto.PositionId) != null;
                    if (!positionExists)
                        return Result.Failure(new Error(ErrorCodes.NotFound, $"The position with ID '{dto.PositionId}' does not exist."));

                    // Optener el emplado actual 

                    // mapear del dto a la entidad empleado 
                    _mapper.Map(dto, employe); // Usa la sobrecarga que mapea de origen a destino

                    employe.UpdatedByUserId = userOnly!.Id;
                    await _employeesRepository.UpdateEmployeeAsync(employe);
                    await _employeesRepository.SaveChangesAsync();
                    await transacion.CommitAsync();

                    return Result.Success();
                }
                catch(DbUpdateException ex )
                {
                    await transacion.RollbackAsync();
                    return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Conflict, $"Un conflicto de datos único ha ocurrido. {ex.Message} "));
                }
            }
            catch ( Exception ex )
            {
                return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
            }
        }

        // Agregar un nuevo empleado 
        public async Task<Result<EmployeReponseDto>> AddEmployesAsync(EmployesCreationDto employes)
        {
            var valitationResult = await _validator.ValidateAsync(employes);
            if (!valitationResult.IsValid)
            {
                var errors = valitationResult.Errors
                    .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                    .ToList();
                return Result<EmployeReponseDto>.Failure(errors);
            }
            try
            {
                using var transacion = await _employeesRepository.BeginTransactionAsync();
                try
                {
                    var userOnly = await _userService.GetCurrentUserAsync();

                    // Verificar Email 
                    var existingEmail = await _employeesRepository.ExistAsync(c => c.NormalizedEmail == employes.Email.ToUpper());
                    if (existingEmail)
                        return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Conflict, $"El correo electrónico '{employes.Email}' ya existe.", "email"));

                    var existingDni = await _employeesRepository.ExistAsync(c => c.Dni == employes.Dni!.ToUpper());
                    if (existingDni)
                        // Usamos el constructor con campo para el error 409
                        return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Conflict, $"El número de cédula '{employes.Dni}' ya existe.", "dni"));

                    // TODO : Falta validar que la edad concuerde con la del dni



                    // Verificar que la especialidad existe ( si es que se mando una )
                    if (employes.SpecialtyId.HasValue)
                    {
                        var specialtyExists = await _specialtiesRepository.GetByIdAsync(employes.SpecialtyId.Value) != null;
                        if (!specialtyExists)
                            return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.NotFound, $"The specialty with ID '{employes.SpecialtyId.Value}' does not exist."));
                    }
                    var positionExists = await _positionRepository.GetByIdAsync(employes.PositionId) != null;
                    if (!positionExists)
                        return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.NotFound, $"The position with ID '{employes.PositionId}' does not exist."));


                    // creacion de un nuevo usuario 
                    var employee = _mapper.Map<Employee>(employes);
                    employee.CreatedByUserId = userOnly?.Id;
                    employee.UpdatedByUserId = userOnly?.Id;
                    await _employeesRepository.AddAsync(employee);
                    await _employeesRepository.SaveChangesAsync();
                    await transacion.CommitAsync();

                    var employeeDto = _mapper.Map<EmployeReponseDto>(employee);
                    return Result<EmployeReponseDto>.Success(employeeDto);

                }
                catch(DbUpdateException ex)
                {
                    await transacion.RollbackAsync();
                    return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Conflict, "Un conflicto de datos único ha ocurrido. Intente de nuevo."));
                }

            }
            catch (Exception ex)
            {
                return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
            }
        }

    }
}
