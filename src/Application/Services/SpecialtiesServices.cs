using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using Domain.Entities;

namespace Application.Services
{
    public class SpecialtiesServices : ISpecialtiesServices
    {
        private readonly ISpecialtiesRepository _specialtiesRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<SpecialtiesDto> _validator;

        public SpecialtiesServices(ISpecialtiesRepository specialtiesRepository , IMapper mapper , IValidator<SpecialtiesDto> validator)
        {
            _specialtiesRepository = specialtiesRepository;
            _mapper = mapper;
            _validator = validator;
        }
        public async Task<Result<Specialty>> AddSpecialtyAsync(SpecialtiesDto specialtiesDto)
        {
            var validationResult = await _validator.ValidateAsync(specialtiesDto);
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
                        new Error(ErrorCodes.Conflict, $"La especialidad con el nombre '{specialtiesDto.Name}' ya existe."));
                }
                var specialty = _mapper.Map<Specialty>(specialtiesDto);
                await _specialtiesRepository.AddAsync(specialty);
                await _specialtiesRepository.SaveChangesAsync();
                return Result<Specialty>.Success(specialty);
            }
            catch(Exception ex)
            {
                return Result<Specialty>.Failure(
                    new Error(ErrorCodes.Unexpected, $"An unexpected error occurred{ex.Message}"));
            }
        }


        public async Task<Result<Specialty>> GetByIdAsync(int id )
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

        public async Task<Result<Specialty>> UpdateSpecialtyAsync(int id, SpecialtiesDto specialtiesDto)
        {
            var validationResult = await _validator.ValidateAsync(specialtiesDto);
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
            // Verificar si el nombre ya existe en otra especialidad
            var existingSpecialty = await _specialtiesRepository.GetByNameAsync(specialtiesDto.Name);
            if (existingSpecialty != null && existingSpecialty.Id != id)
            {
                return Result<Specialty>.Failure(new Error(ErrorCodes.Conflict, $"A specialty with the name '{specialtiesDto.Name}' already exists."));
            }

            // Mapear los campos actualizados
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
}
