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
        private readonly ISpecialtiesRepository _specialtiesRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IUserService _userService;

        public EmployesServices
        (
            IEmployesRepository employeesRepository,
            IUserRepository userRepository,
            IMapper mapper,
            IValidator<EmployesCreationDto> validator,
            ISpecialtiesRepository specialtiesRepository,
            IPositionRepository positionRepository,
            IUserService userService
        )
        {
            _employeesRepository = employeesRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _validator = validator;
            _specialtiesRepository = specialtiesRepository;
            _positionRepository = positionRepository;
            _userService = userService;
        }

        // Obtener el empleado por el id  return CreatedAtRoute("ObtenerAutor",new {id = autor.Id}, autorDTO);
        public async Task<Result<EmployeReponseDto>> GetEmployeeById(int Id)
        {
            try
            {
                if (Id <= 0) return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.BadRequest, $"The ID : {Id} sent is not valid."));

                var employee = await _employeesRepository.GetByIdAsync(Id);
                if (employee == null)
                {
                    return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.NotFound, $"Employee {Id} not found"));
                }
                var employeeDto = _mapper.Map<EmployeReponseDto>(employee);
                return Result<EmployeReponseDto>.Success(employeeDto);
            }
            catch ( Exception ex )
            {
                return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
            }
        }
        
        public async Task<Result<PaginatedResponseDto<EmployeeListDTO>>> GetAllEmployes(PaginationDto pagination)
        {
            try
            {
                var (baseQuery, total) = await _employeesRepository.GetQueryAndTotal( include : q => q.Include(e => e.Position).Include(e => e.Specialty));
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


        public async Task<Result<EmployeReponseDto>> AddSpecialtyAsync(EmployesCreationDto specialtiesDto)
        {
            var valitationResult = await _validator.ValidateAsync(specialtiesDto);
            if (!valitationResult.IsValid)
            {
                var errors = valitationResult.Errors
                    .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                    .ToList();
                return Result<EmployeReponseDto>.Failure(errors);
            }
            try
            {
                var userOnly = await _userService.GetCurrentUserAsync();

                var existingEmail = await _userRepository.ExistAsync(c => c.Email == specialtiesDto.Email);
                if (existingEmail)
                    return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Conflict, $"El correo electronico '{specialtiesDto.Email}' ya existe."));
                
                var existingDni = await _employeesRepository.ExistAsync(c => c.Dni == specialtiesDto.Dni!.ToUpper());
                if (existingDni)
                    return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Conflict, $"The ID number '{specialtiesDto.Dni}' already exists."));

                // TODO : Falta validar que la edad concuerde con la del dni



                // Verificar que la especialidad existe ( si es que se mando una )
                if (specialtiesDto.SpecialtyId.HasValue)
                {
                    var specialtyExists = await _specialtiesRepository.GetByIdAsync(specialtiesDto.SpecialtyId.Value) != null;
                    if (!specialtyExists)
                        return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.NotFound, $"The specialty with ID '{specialtiesDto.SpecialtyId.Value}' does not exist."));
                }
                var positionExists = await _positionRepository.GetByIdAsync(specialtiesDto.PositionId) != null;
                if (!positionExists)
                    return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.NotFound, $"The position with ID '{specialtiesDto.PositionId}' does not exist."));

                
                // creacion de un nuevo usuario 
                var employee = _mapper.Map<Employee>(specialtiesDto);
                employee.CreatedByUserId = userOnly?.Id;
                employee.UpdatedByUserId = userOnly?.Id;
                await _employeesRepository.AddAsync(employee);
                await _employeesRepository.SaveChangesAsync();
                var employeeDto = _mapper.Map<EmployeReponseDto>(employee);
                return Result<EmployeReponseDto>.Success(employeeDto);

            }
            catch (Exception ex)
            {
                return Result<EmployeReponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
            }
        }

    }
}
