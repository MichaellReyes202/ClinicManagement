using Application.DTOs.Appointment;
using Application.DTOs.Employee;
using Application.DTOs.ExamType;
using Application.DTOs.Patient;
using Application.DTOs.Position;
using Application.DTOs.Role;
using Application.DTOs.specialty;
using Application.DTOs.User;
using Application.DTOs.Medication;
using Application.DTOs.Prescription;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappers
{
    public class AutoMapperProfiles : Profile
    {
        //   CreateMap<Fuente, Destino>()  
        public AutoMapperProfiles()
        {
            CreateMap<Role, RoleDto>();

            CreateMap<UserDto, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                // ultimo login 
                .ForMember(dest => dest.LastLogin, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // La contraseña se maneja por UserManager
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.LockoutEnabled, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.AccessFailedCount, opt => opt.MapFrom(src => 0));


            // Mapeo para la creacion de las especialidad 
            CreateMap<SpecialtiesCreateDto, Specialty>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Employees, opt => opt.Ignore());

            // Mapeo para la actualizacion de la especialidad 
            CreateMap<SpecialtiesUpdateDto, Specialty>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));


            // Mapeo para la creacion del cargo 
            CreateMap<PositionCreationDto , Position>()
                .ForMember(dest => dest.Name , opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description , opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Employees, opt => opt.Ignore());

            // Mapeo para la actualizacion del cargo 
            CreateMap<PositionUpdateDto , Position>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));



            // RoleDto to Role mapping
            CreateMap<RoleDto, Role>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

            
            CreateMap<UserDto, User>()
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // La contraseña se maneja por UserManager
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.LockoutEnabled, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.AccessFailedCount, opt => opt.MapFrom(src => 0));

            // EmployesCreationDto to Employee mapping
            CreateMap<EmployesCreationDto, Employee>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Se establece al crear el usuario
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.SecondLastName, opt => opt.MapFrom(src => src.SecondLastName))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age)) // Validar con la edad del dni
                .ForMember(dest => dest.PositionId, opt => opt.MapFrom(src => src.PositionId))
                .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone))
                .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.HireDate, DateTimeKind.Utc)))
                .ForMember(dest => dest.Dni, opt => opt.MapFrom(src => src.Dni!.ToUpper()))
                .ForMember(dest => dest.SpecialtyId, opt => opt.MapFrom(src => src.SpecialtyId))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src!.Email))
                .ForMember(dest => dest.NormalizedEmail, opt => opt.MapFrom(src => src.Email.ToUpper()))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore()) //  será establecido en el servicio
                .ForMember(dest => dest.UpdatedByUserId, opt => opt.Ignore()) //  será establecido en el servicio
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Position, opt => opt.Ignore())
                .ForMember(dest => dest.Specialty, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedByUser, opt => opt.Ignore());

            // Employee to EmployeeDto mapping
            CreateMap<Employee, EmployeReponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.SecondLastName, opt => opt.MapFrom(src => src.SecondLastName))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
                .ForMember(dest => dest.PositionId, opt => opt.MapFrom(src => src.PositionId))
                .ForMember(dest => dest.SpecialtyId, opt => opt.MapFrom(src => src.SpecialtyId))
                .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone))
                .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => DateOnly.FromDateTime(src.HireDate)))
                .ForMember(dest => dest.Dni, opt => opt.MapFrom(src => src.Dni))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));

            // EmployeeUpdateDto  to  Employee
            CreateMap<EmployesUpdateDto, Employee>()
               .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
               .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
               .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
               .ForMember(dest => dest.SecondLastName, opt => opt.MapFrom(src => src.SecondLastName))
               .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
               .ForMember(dest => dest.PositionId, opt => opt.MapFrom(src => src.PositionId))
               .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone))
               .ForMember(dest => dest.HireDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.HireDate, DateTimeKind.Utc)))
               .ForMember(dest => dest.Dni, opt => opt.MapFrom(src => src.Dni))
               .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
               .ForMember(dest => dest.NormalizedEmail,opt => opt.MapFrom(src => src.Email.ToUpper()))
               .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
               .ForMember(dest => dest.SpecialtyId, opt => opt.MapFrom(src => src.SpecialtyId))
               .ForMember(dest => dest.UserId, opt => opt.Ignore())
               .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
               .ForMember(dest => dest.UpdatedByUserId, opt => opt.Ignore())
               .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
               .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));


            // -------------------------------------- Pacientes  --------------------------------------

            CreateMap<PatientCreateDto, Patient>()
               .ForMember(dest => dest.FirstName            , opt => opt.MapFrom(src => src.FirstName))
               .ForMember(dest => dest.MiddleName           , opt => opt.MapFrom(src => src.MiddleName))
               .ForMember(dest => dest.LastName             , opt => opt.MapFrom(src => src.LastName))
               .ForMember(dest => dest.SecondLastName       , opt => opt.MapFrom(src => src.SecondLastName))
               .ForMember(dest => dest.DateOfBirth          , opt => opt.MapFrom(src => src.DateOfBirth))
               .ForMember(dest => dest.Dni                  , opt => opt.MapFrom(src => src.Dni))
               .ForMember(dest => dest.ContactPhone         , opt => opt.MapFrom(src => src.ContactPhone))
               .ForMember(dest => dest.ContactEmail         , opt => opt.MapFrom(src => src.ContactEmail))
               .ForMember(dest => dest.Address              , opt => opt.MapFrom(src => src.Address))
               .ForMember(dest => dest.SexId                , opt => opt.MapFrom(src => src.SexId))
               .ForMember(dest => dest.BloodTypeId          , opt => opt.MapFrom(src => src.BloodTypeId))
               .ForMember(dest => dest.ConsultationReasons  , opt => opt.MapFrom(src => src.ConsultationReasons))
               .ForMember(dest => dest.ChronicDiseases      , opt => opt.MapFrom(src => src.ChronicDiseases))
               .ForMember(dest => dest.Allergies            , opt => opt.MapFrom(src => src.Allergies))
               .ForMember(dest => dest.CreatedByUserId      , opt => opt.Ignore())
               .ForMember(dest => dest.UpdatedByUser        , opt => opt.Ignore())
               .ForMember(dest => dest.PatientGuardian, opt => opt.MapFrom(src =>
                   src.Guardian != null
                       ? new PatientGuardian
                       {
                           FullName = src.Guardian.FullName,
                           Dni = src.Guardian.Dni,
                           Relationship = src.Guardian.Relationship,
                           ContactPhone = src.Guardian.ContactPhone,
                           CreatedAt = DateTime.UtcNow
                       }
                       : null))
               .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
               .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<PatientUpdateDto, Patient>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
               .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
               .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
               .ForMember(dest => dest.SecondLastName, opt => opt.MapFrom(src => src.SecondLastName))
               .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
               .ForMember(dest => dest.Dni, opt => opt.MapFrom(src => src.Dni))
               .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone))
               .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.ContactEmail))
               .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
               .ForMember(dest => dest.SexId, opt => opt.MapFrom(src => src.SexId))
               .ForMember(dest => dest.BloodTypeId, opt => opt.MapFrom(src => src.BloodTypeId))
               .ForMember(dest => dest.ConsultationReasons, opt => opt.MapFrom(src => src.ConsultationReasons))
               .ForMember(dest => dest.ChronicDiseases, opt => opt.MapFrom(src => src.ChronicDiseases))
               .ForMember(dest => dest.Allergies, opt => opt.MapFrom(src => src.Allergies))
               .ForMember(dest => dest.PatientGuardian, opt => opt.Ignore()) // se maneja manualmente en el servicio
               .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
               .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));


            CreateMap<PatientGuardianDto, PatientGuardian>()
               .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
               .ForMember(dest => dest.Dni, opt => opt.MapFrom(src => src.Dni))
               .ForMember(dest => dest.Relationship, opt => opt.MapFrom(src => src.Relationship))
               .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone))
               .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<Patient , PatientResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.SecondLastName, opt => opt.MapFrom(src => src.SecondLastName))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.Dni, opt => opt.MapFrom(src => src.Dni))
                .ForMember(dest => dest.ContactPhone, opt => opt.MapFrom(src => src.ContactPhone))
                .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.ContactEmail))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.SexId, opt => opt.MapFrom(src => src.SexId))
                .ForMember(dest => dest.BloodTypeId, opt => opt.MapFrom(src => src.BloodTypeId))
                .ForMember(dest => dest.ConsultationReasons, opt => opt.MapFrom(src => src.ConsultationReasons))
                .ForMember(dest => dest.ChronicDiseases, opt => opt.MapFrom(src => src.ChronicDiseases))
                .ForMember(dest => dest.Allergies, opt => opt.MapFrom(src => src.Allergies))
                .ForMember(dest => dest.Guardian, opt => opt.MapFrom(src =>
                     src.PatientGuardian != null
                          ? new PatientGuardianDto
                          {
                            FullName = src.PatientGuardian.FullName,
                            Dni = src.PatientGuardian.Dni,
                            Relationship = src.PatientGuardian.Relationship,
                            ContactPhone = src.PatientGuardian.ContactPhone
                          }
                          : null));


            // -------------------------------------- Tipos de examenes   --------------------------------------

            // fuente , destino 
            CreateMap<ExamTypeCreateDto, ExamType>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.DeliveryTime, opt => opt.MapFrom(src => src.DeliveryTime))
                .ForMember(dest => dest.PricePaid, opt => opt.MapFrom(src => src.PricePaid))
                .ForMember(dest => dest.SpecialtyId, opt => opt.MapFrom(src => src.SpecialtyId))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))

                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // se manejara desde el servicio
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) // se manejara desde el servicio
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore()) // se manejara desde el servicio
                .ForMember(dest => dest.UpdatedByUserId, opt => opt.Ignore()) // se manejara desde el servicio
                .ReverseMap();

            CreateMap<ExamTypeUpdateDto, ExamType>();
            CreateMap<ExamType, ExamTypeResponseDto>();


            // ---------------------------------- Appointment --------------------------------

            CreateMap<AppointmentCreateDto, Appointment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.StatusId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.EndTime, opt => opt.Ignore());

            CreateMap<Appointment, AppointmentResponseDto>();

            // ---------------------------------- Medication --------------------------------
            CreateMap<Medication, MedicationDto>();

            // ---------------------------------- Prescription --------------------------------
            CreateMap<Prescription, PrescriptionDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.PrescriptionItems))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Consultation != null && src.Consultation.Employee != null ? $"{src.Consultation.Employee.FirstName} {src.Consultation.Employee.LastName}" : null));

            CreateMap<PrescriptionItem, PrescriptionItemDto>()
                .ForMember(dest => dest.MedicationName, opt => opt.MapFrom(src => src.Medication != null ? src.Medication.Name : string.Empty))
                .ForMember(dest => dest.Concentration, opt => opt.MapFrom(src => src.Medication != null ? src.Medication.Concentration : null));
        }
    }
}
