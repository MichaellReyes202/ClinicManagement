using System;
using System.Collections.Generic;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public partial class ClinicDbContext : DbContext
{
    public ClinicDbContext()
    {
    }

    public ClinicDbContext(DbContextOptions<ClinicDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Auditlog> Auditlogs { get; set; }

    public virtual DbSet<CatAppointmentStatus> CatAppointmentStatuses { get; set; }

    public virtual DbSet<CatAuditModule> CatAuditModules { get; set; }

    public virtual DbSet<CatAuditStatus> CatAuditStatuses { get; set; }

    public virtual DbSet<CatBloodType> CatBloodTypes { get; set; }

    public virtual DbSet<CatExamsStatus> CatExamsStatuses { get; set; }

    public virtual DbSet<CatSexo> CatSexos { get; set; }

    public virtual DbSet<Consultation> Consultations { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<ExamType> ExamTypes { get; set; }

    public virtual DbSet<Medication> Medications { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PatientGuardian> PatientGuardians { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<PrescriptionItem> PrescriptionItems { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionExam> PromotionExams { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Specialty> Specialties { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=ep-blue-tree-aed8hhdt-pooler.c-2.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_jGxmaYBT1QN6;SSL Mode=Require");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("appointment_status_type", new[] { "Programada", "Confirmada", "Cancelada", "Reprogramada", "Realizada" })
            .HasPostgresExtension("pg_trgm")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("appointments_pkey");

            entity.ToTable("appointments");

            entity.HasIndex(e => e.EmployeeId, "idx_appointments_employee_id");

            entity.HasIndex(e => e.StartTime, "idx_appointments_start_time");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Duration)
                .HasDefaultValue(30)
                .HasColumnName("duration");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(250)
                .HasColumnName("reason");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.AppointmentCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("appointments_created_by_user_id_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("appointments_employee_id_fkey");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("appointments_patient_id_fkey");

            entity.HasOne(d => d.Status).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("appointments_status_id_fkey");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.AppointmentUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("appointments_updated_by_user_id_fkey");
        });

        modelBuilder.Entity<Auditlog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auditlog_pkey");

            entity.ToTable("auditlog");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Actiontype)
                .HasMaxLength(50)
                .HasColumnName("actiontype");
            entity.Property(e => e.Changedetail).HasColumnName("changedetail");
            entity.Property(e => e.Moduleid).HasColumnName("moduleid");
            entity.Property(e => e.Performedbyuserid).HasColumnName("performedbyuserid");
            entity.Property(e => e.Recorddisplay)
                .HasMaxLength(255)
                .HasColumnName("recorddisplay");
            entity.Property(e => e.Recordid).HasColumnName("recordid");
            entity.Property(e => e.Statusid).HasColumnName("statusid");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("now()")
                .HasColumnName("timestamp");

            entity.HasOne(d => d.Module).WithMany(p => p.Auditlogs)
                .HasForeignKey(d => d.Moduleid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_auditlogsimple_module");

            entity.HasOne(d => d.Performedbyuser).WithMany(p => p.Auditlogs)
                .HasForeignKey(d => d.Performedbyuserid)
                .HasConstraintName("fk_auditlogsimple_user");

            entity.HasOne(d => d.Status).WithMany(p => p.Auditlogs)
                .HasForeignKey(d => d.Statusid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_auditlogsimple_status");
        });

        modelBuilder.Entity<CatAppointmentStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cat_appointment_status_pkey");

            entity.ToTable("cat_appointment_status");

            entity.HasIndex(e => e.Name, "cat_appointment_status_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
        });

        modelBuilder.Entity<CatAuditModule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cat_audit_module_pkey");

            entity.ToTable("cat_audit_module");

            entity.HasIndex(e => e.Name, "cat_audit_module_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<CatAuditStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cat_audit_status_pkey");

            entity.ToTable("cat_audit_status");

            entity.HasIndex(e => e.Name, "cat_audit_status_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<CatBloodType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cat_blood_types_pkey");

            entity.ToTable("cat_blood_types");

            entity.HasIndex(e => e.Name, "cat_blood_types_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(10)
                .HasColumnName("name");
        });

        modelBuilder.Entity<CatExamsStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cat_exams_status_pkey");

            entity.ToTable("cat_exams_status");

            entity.HasIndex(e => e.Name, "cat_exams_status_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
        });

        modelBuilder.Entity<CatSexo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cat_sexos_pkey");

            entity.ToTable("cat_sexos");

            entity.HasIndex(e => e.Name, "cat_sexos_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(15)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Consultation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("consultations_pkey");

            entity.ToTable("consultations");

            entity.HasIndex(e => e.AppointmentId, "consultations_appointment_id_key").IsUnique();

            entity.HasIndex(e => e.CreatedAt, "idx_consultations_date");

            entity.HasIndex(e => e.EmployeeId, "idx_consultations_employee");

            entity.HasIndex(e => e.PatientId, "idx_consultations_patient");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Diagnosis).HasColumnName("diagnosis");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.FinalizedAt).HasColumnName("finalized_at");
            entity.Property(e => e.IsFinalized)
                .HasDefaultValue(false)
                .HasColumnName("is_finalized");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.PhysicalExam).HasColumnName("physical_exam");
            entity.Property(e => e.Reason)
                .HasMaxLength(250)
                .HasColumnName("reason");
            entity.Property(e => e.TreatmentNotes).HasColumnName("treatment_notes");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.Appointment).WithOne(p => p.Consultation)
                .HasForeignKey<Consultation>(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("consultations_appointment_id_fkey");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ConsultationCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("consultations_created_by_user_id_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("consultations_employee_id_fkey");

            entity.HasOne(d => d.Patient).WithMany(p => p.Consultations)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("consultations_patient_id_fkey");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.ConsultationUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("consultations_updated_by_user_id_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employees_pkey");

            entity.ToTable("employees");

            entity.HasIndex(e => e.Dni, "employees_dni_key").IsUnique();

            entity.HasIndex(e => e.Email, "employees_email_key").IsUnique();

            entity.HasIndex(e => e.NormalizedEmail, "employees_normalized_email_key").IsUnique();

            entity.HasIndex(e => e.UserId, "employees_user_id_key").IsUnique();

            entity.HasIndex(e => e.PositionId, "idx_employees_position_id");

            entity.HasIndex(e => e.UserId, "idx_employees_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(8)
                .HasColumnName("contact_phone");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Dni)
                .HasMaxLength(20)
                .HasColumnName("dni");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.HireDate).HasColumnName("hire_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(100)
                .HasColumnName("middle_name");
            entity.Property(e => e.NormalizedEmail)
                .HasMaxLength(255)
                .HasColumnName("normalized_email");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.SecondLastName)
                .HasMaxLength(100)
                .HasColumnName("second_last_name");
            entity.Property(e => e.SpecialtyId).HasColumnName("specialty_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.EmployeeCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("employees_created_by_user_id_fkey");

            entity.HasOne(d => d.Position).WithMany(p => p.Employees)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("employees_position_id_fkey");

            entity.HasOne(d => d.Specialty).WithMany(p => p.Employees)
                .HasForeignKey(d => d.SpecialtyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("employees_specialty_id_fkey");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.EmployeeUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("employees_updated_by_user_id_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.EmployeeUser)
                .HasForeignKey<Employee>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("employees_user_id_fkey");
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("exams_pkey");

            entity.ToTable("exams");

            entity.HasIndex(e => e.AppointmentId, "idx_exams_appointment_id");

            entity.HasIndex(e => e.ConsultationId, "idx_exams_consultation_id");

            entity.HasIndex(e => e.ExamTypeId, "idx_exams_exam_type_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppointmentId).HasColumnName("appointment_id");
            entity.Property(e => e.ConsultationId).HasColumnName("consultation_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExamTypeId).HasColumnName("exam_type_id");
            entity.Property(e => e.PerformedByEmployeeId).HasColumnName("performed_by_employee_id");
            entity.Property(e => e.Results).HasColumnName("results");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Appointment).WithMany(p => p.Exams)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("exams_appointment_id_fkey");

            entity.HasOne(d => d.Consultation).WithMany(p => p.Exams)
                .HasForeignKey(d => d.ConsultationId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("exams_consultation_id_fkey");

            entity.HasOne(d => d.ExamType).WithMany(p => p.Exams)
                .HasForeignKey(d => d.ExamTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("exams_exam_type_id_fkey");

            entity.HasOne(d => d.PerformedByEmployee).WithMany(p => p.Exams)
                .HasForeignKey(d => d.PerformedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("exams_performed_by_employee_id_fkey");

            entity.HasOne(d => d.Status).WithMany(p => p.Exams)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("exams_status_id_fkey");
        });

        modelBuilder.Entity<ExamType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("exam_types_pkey");

            entity.ToTable("exam_types");

            entity.HasIndex(e => e.Name, "exam_types_name_key").IsUnique();

            entity.HasIndex(e => e.SpecialtyId, "idx_exam_types_specialty_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.DeliveryTime).HasColumnName("delivery_time");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PricePaid)
                .HasPrecision(10, 2)
                .HasColumnName("price_paid");
            entity.Property(e => e.SpecialtyId).HasColumnName("specialty_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ExamTypeCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("exam_types_created_by_user_id_fkey");

            entity.HasOne(d => d.Specialty).WithMany(p => p.ExamTypes)
                .HasForeignKey(d => d.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("exam_types_specialty_id_fkey");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.ExamTypeUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("exam_types_updated_by_user_id_fkey");
        });

        modelBuilder.Entity<Medication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("medications_pkey");

            entity.ToTable("medications");

            entity.HasIndex(e => e.GenericName, "idx_medications_generic");

            entity.HasIndex(e => e.Name, "idx_medications_name");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Concentration)
                .HasMaxLength(100)
                .HasColumnName("concentration");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.GenericName)
                .HasMaxLength(255)
                .HasColumnName("generic_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Presentation)
                .HasMaxLength(100)
                .HasColumnName("presentation");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0.00")
                .HasColumnName("price");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("patients_pkey");

            entity.ToTable("patients");

            entity.HasIndex(e => new { e.FirstName, e.LastName }, "idx_patients_name_search")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops", "gin_trgm_ops" });

            entity.HasIndex(e => e.ContactEmail, "patients_contact_email_key").IsUnique();

            entity.HasIndex(e => e.Dni, "patients_dni_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Allergies).HasColumnName("allergies");
            entity.Property(e => e.BloodTypeId).HasColumnName("blood_type_id");
            entity.Property(e => e.ChronicDiseases).HasColumnName("chronic_diseases");
            entity.Property(e => e.ConsultationReasons).HasColumnName("consultation_reasons");
            entity.Property(e => e.ContactEmail)
                .HasMaxLength(255)
                .HasColumnName("contact_email");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(50)
                .HasColumnName("contact_phone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Dni)
                .HasMaxLength(20)
                .HasColumnName("dni");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(50)
                .HasColumnName("middle_name");
            entity.Property(e => e.SecondLastName)
                .HasMaxLength(50)
                .HasColumnName("second_last_name");
            entity.Property(e => e.SexId).HasColumnName("sex_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.BloodType).WithMany(p => p.Patients)
                .HasForeignKey(d => d.BloodTypeId)
                .HasConstraintName("patients_blood_type_id_fkey");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.PatientCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("patients_created_by_user_id_fkey");

            entity.HasOne(d => d.Sex).WithMany(p => p.Patients)
                .HasForeignKey(d => d.SexId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("patients_sex_id_fkey");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.PatientUpdatedByUsers)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("patients_updated_by_user_id_fkey");
        });

        modelBuilder.Entity<PatientGuardian>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("patient_guardians_pkey");

            entity.ToTable("patient_guardians");

            entity.HasIndex(e => e.PatientId, "idx_guardians_patient_id");

            entity.HasIndex(e => e.PatientId, "patient_guardians_patient_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(8)
                .HasColumnName("contact_phone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Dni)
                .HasMaxLength(20)
                .HasColumnName("dni");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.Relationship)
                .HasMaxLength(100)
                .HasColumnName("relationship");

            entity.HasOne(d => d.Patient).WithOne(p => p.PatientGuardian)
                .HasForeignKey<PatientGuardian>(d => d.PatientId)
                .HasConstraintName("patient_guardians_patient_id_fkey");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("positions_pkey");

            entity.ToTable("positions");

            entity.HasIndex(e => e.Name, "idx_positions_name");

            entity.HasIndex(e => e.Name, "positions_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("prescriptions_pkey");

            entity.ToTable("prescriptions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConsultationId).HasColumnName("consultation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'emitida'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Consultation).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.ConsultationId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("prescriptions_consultation_id_fkey");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("prescriptions_created_by_user_id_fkey");
        });

        modelBuilder.Entity<PrescriptionItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("prescription_items_pkey");

            entity.ToTable("prescription_items");

            entity.HasIndex(e => e.PrescriptionId, "idx_presc_items_prescription");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Dose)
                .HasMaxLength(100)
                .HasColumnName("dose");
            entity.Property(e => e.Duration)
                .HasMaxLength(100)
                .HasColumnName("duration");
            entity.Property(e => e.Frequency)
                .HasMaxLength(100)
                .HasColumnName("frequency");
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.MedicationId).HasColumnName("medication_id");
            entity.Property(e => e.PrescriptionId).HasColumnName("prescription_id");
            entity.Property(e => e.TotalQuantity).HasColumnName("total_quantity");

            entity.HasOne(d => d.Medication).WithMany(p => p.PrescriptionItems)
                .HasForeignKey(d => d.MedicationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("prescription_items_medication_id_fkey");

            entity.HasOne(d => d.Prescription).WithMany(p => p.PrescriptionItems)
                .HasForeignKey(d => d.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("prescription_items_prescription_id_fkey");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("promotions_pkey");

            entity.ToTable("promotions");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "idx_promotions_dates");

            entity.HasIndex(e => e.Name, "promotions_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DiscountPercentage)
                .HasPrecision(5, 2)
                .HasColumnName("discount_percentage");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.FixedPrice)
                .HasPrecision(10, 2)
                .HasColumnName("fixed_price");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("promotions_created_by_user_id_fkey");
        });

        modelBuilder.Entity<PromotionExam>(entity =>
        {
            entity.HasKey(e => new { e.PromotionId, e.ExamTypeId }).HasName("promotion_exams_pkey");

            entity.ToTable("promotion_exams");

            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.ExamTypeId).HasColumnName("exam_type_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");

            entity.HasOne(d => d.ExamType).WithMany(p => p.PromotionExams)
                .HasForeignKey(d => d.ExamTypeId)
                .HasConstraintName("promotion_exams_exam_type_id_fkey");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionExams)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("promotion_exams_promotion_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.Name, "roles_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("specialties_pkey");

            entity.ToTable("specialties");

            entity.HasIndex(e => e.Name, "specialties_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.NormalizedEmail, "idx_users_normalized_email");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.NormalizedEmail, "users_normalized_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccessFailedCount)
                .HasDefaultValue(0)
                .HasColumnName("access_failed_count");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.LockoutEnabled)
                .HasDefaultValue(false)
                .HasColumnName("lockout_enabled");
            entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
            entity.Property(e => e.NormalizedEmail)
                .HasColumnType("character varying")
                .HasColumnName("normalized_email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.InverseCreatedByUser)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_created_by_user_id_fkey");

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.InverseUpdatedByUser)
                .HasForeignKey(d => d.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_updated_by_user_id_fkey");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("user_roles_pkey");

            entity.ToTable("user_roles");

            entity.HasIndex(e => e.RoleId, "idx_user_roles_role_id");

            entity.HasIndex(e => e.UserId, "idx_user_roles_user_id");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.UserRoleCreatedByUsers)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("user_roles_created_by_user_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("user_roles_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoleUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_roles_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
