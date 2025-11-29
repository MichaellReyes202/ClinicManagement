using Application.DTOs.Prescription;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;

namespace Application.Services;

public class PrescriptionServices : IPrescriptionServices
{
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IConsultationRepository _consultationRepository;
    private readonly IMapper _mapper;

    public PrescriptionServices(
        IPrescriptionRepository prescriptionRepository,
        IConsultationRepository consultationRepository,
        IMapper mapper)
    {
        _prescriptionRepository = prescriptionRepository;
        _consultationRepository = consultationRepository;
        _mapper = mapper;
    }

    public async Task<Result<PrescriptionDto>> CreatePrescriptionAsync(CreatePrescriptionDto dto)
    {
        var consultation = await _consultationRepository.GetByIdAsync(dto.ConsultationId);
        if (consultation == null)
        {
            return Result<PrescriptionDto>.Failure(new Error("Consultation.NotFound", "Consultation not found"));
        }

        // Check if prescription already exists for this consultation
        var existingPrescription = await _prescriptionRepository.GetByConsultationIdAsync(dto.ConsultationId);
        if (existingPrescription != null)
        {
             return Result<PrescriptionDto>.Failure(new Error("Prescription.Exists", "Prescription already exists for this consultation"));
        }

        var prescription = new Prescription
        {
            ConsultationId = dto.ConsultationId,
            Notes = dto.Notes,
            Status = "emitida",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var itemDto in dto.Items)
        {
            prescription.PrescriptionItems.Add(new PrescriptionItem
            {
                MedicationId = itemDto.MedicationId,
                Dose = itemDto.Dose,
                Frequency = itemDto.Frequency,
                Duration = itemDto.Duration,
                TotalQuantity = itemDto.TotalQuantity,
                Instructions = itemDto.Instructions,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _prescriptionRepository.AddAsync(prescription);
        await _prescriptionRepository.SaveChangesAsync();

        // Fetch back to get included data if needed, or just map what we have
        var prescriptionDto = _mapper.Map<PrescriptionDto>(prescription);
        return Result<PrescriptionDto>.Success(prescriptionDto);
    }

    public async Task<Result<PrescriptionDto>> GetByConsultationIdAsync(int consultationId)
    {
        var prescription = await _prescriptionRepository.GetByConsultationIdAsync(consultationId);
        if (prescription == null)
        {
            return Result<PrescriptionDto>.Failure(new Error("Prescription.NotFound", "Prescription not found"));
        }

        var dto = _mapper.Map<PrescriptionDto>(prescription);
        return Result<PrescriptionDto>.Success(dto);
    }

    public async Task<Result<IEnumerable<PrescriptionDto>>> GetByPatientIdAsync(int patientId)
    {
        var prescriptions = await _prescriptionRepository.GetByPatientIdAsync(patientId);
        var dtos = _mapper.Map<IEnumerable<PrescriptionDto>>(prescriptions);
        return Result<IEnumerable<PrescriptionDto>>.Success(dtos);
    }
}
