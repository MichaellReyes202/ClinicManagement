using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;

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
            IEmployesRepository employeesRepository , 
            IUserRepository userRepository , 
            IMapper mapper,
            IValidator<EmployesCreationDto> validator ,
            ISpecialtiesRepository specialtiesRepository ,
            IPositionRepository positionRepository ,
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

        // Metodo para crear un empleado 
        public async Task<Result<EmployesReponseDto>> AddSpecialtyAsync(EmployesCreationDto specialtiesDto)
        {
            var valitationResult = await _validator.ValidateAsync(specialtiesDto);
            if (!valitationResult.IsValid)
            {
                var errors = valitationResult.Errors
                    .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                    .ToList();
                return Result<EmployesReponseDto>.Failure(errors);
            }
            try
            {
                var userOnly = await _userService.GetCurrentUserAsync();

                var existingEmail = await _userRepository.ExistAsync(c => c.Email == specialtiesDto.Email);
                if (existingEmail)
                    return Result<EmployesReponseDto>.Failure(new Error(ErrorCodes.Conflict, $"El correo electronico '{specialtiesDto.Email}' ya existe."));
                
                var existingDni = await _employeesRepository.ExistAsync(c => c.Dni == specialtiesDto.Dni!.ToUpper());
                if (existingDni)
                    return Result<EmployesReponseDto>.Failure(new Error(ErrorCodes.Conflict, $"The ID number '{specialtiesDto.Dni}' already exists."));

                // TODO : Falta validar que la edad concuerde con la del dni



                // Verificar que la especialidad existe ( si es que se mando una )
                if (specialtiesDto.SpecialtyId.HasValue)
                {
                    var specialtyExists = await _specialtiesRepository.GetByIdAsync(specialtiesDto.SpecialtyId.Value) != null;
                    if (!specialtyExists)
                        return Result<EmployesReponseDto>.Failure(new Error(ErrorCodes.NotFound, $"The specialty with ID '{specialtiesDto.SpecialtyId.Value}' does not exist."));
                }
                var positionExists = await _positionRepository.GetByIdAsync(specialtiesDto.PositionId) != null;
                if (!positionExists)
                    return Result<EmployesReponseDto>.Failure(new Error(ErrorCodes.NotFound, $"The position with ID '{specialtiesDto.PositionId}' does not exist."));

                
                // creacion de un nuevo usuario 
                var employee = _mapper.Map<Employee>(specialtiesDto);
                employee.CreatedByUserId = userOnly?.Id;
                employee.UpdatedByUserId = userOnly?.Id;
                await _employeesRepository.AddAsync(employee);
                await _employeesRepository.SaveChangesAsync();
                var employeeDto = _mapper.Map<EmployesReponseDto>(employee);
                return Result<EmployesReponseDto>.Success(employeeDto);

            }
            catch (Exception ex)
            {
                return Result<EmployesReponseDto>.Failure(new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
            }
        }
    }
}
