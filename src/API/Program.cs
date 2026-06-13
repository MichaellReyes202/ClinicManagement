using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Filters;
using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validators.Auth;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Store;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Area para agregar los filtros personalizados
builder.Services.AddScoped<AuthorizationAuditFilter>();

// Agregar servicios al contenedor
builder
    .Services.AddControllers(options =>
    {
        // Esto asegura que el filtro se ejecute en todas las peticiones
        options.Filters.Add(typeof(AuthorizationAuditFilter));
    })
    .AddJsonOptions(options =>
    {
        // Ignora los ciclos de referencia
        options.JsonSerializerOptions.ReferenceHandler = System
            .Text
            .Json
            .Serialization
            .ReferenceHandler
            .IgnoreCycles;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Configurar DbContext con PostgreSQL
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(defaultConnection))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddDbContext<ClinicDbContext>(options =>
{
    options.UseNpgsql(
        defaultConnection,
        npgsqlOptions =>
        {
            // Reintentos automáticos ante fallos transitorios (cold-start de Neon.tech)
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null
            );
            // Timeout extendido para la autenticación en cold-start
            npgsqlOptions.CommandTimeout(30);
        }
    );
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        // 1. Extraemos los errores automáticos de .NET
        var errors = context
            .ModelState.Where(e => e.Value != null && e.Value.Errors.Count > 0)
            .SelectMany(kvp =>
                kvp.Value!.Errors.Select(e => new ValidationError(
                    propertyName: kvp.Key,
                    errorMessage: e.ErrorMessage
                ))
            )
            .ToList();

        // 2. Armamos nuestro objeto estándar
        var standardError = new Error(
            code: "BadRequest",
            description: "",
            field: null,
            metadata: null,
            validationErrors: errors
        );

        // 3. Retornamos 400 Bad Request con nuestra estructura exacta
        return new BadRequestObjectResult(standardError);

        //var validationErrors = context.ModelState
        //   .Where(ms => ms.Value?.Errors.Count > 0)
        //   .SelectMany(kvp => kvp.Value!.Errors.Select(e =>
        //       new ValidationError(kvp.Key, e.ErrorMessage)))
        //   .ToList();
        //var result = Result.Failure(validationErrors);

        //return new BadRequestObjectResult(new
        //{
        //    message = "Validation failed - check required fields",
        //    errors = result.ValidationErrors.Select(v => new
        //    {
        //        propertyName = v.PropertyName,
        //        errorMessage = v.ErrorMessage
        //    })
        //});
    };
});

// ======================================================
// 1. Añadir el servicio CORS
// ======================================================

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5174", "http://localhost:5173", "https://z6jg3mh4-5174.use.devtunnels.ms" };

    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials(); // si usas cookies o tokens Bearer
        }
    );
});

builder.Services.AddScoped<IPositionServices, PositionServices>();
builder.Services.AddScoped<ICatBloodRepository, CatBloodRepository>();
builder.Services.AddScoped<ISexRepository, SexRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IExamTypeRepository, ExamTypeRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<ICatAppointmentStatusRepository, CatAppointmentStatusRepository>();
builder.Services.AddScoped<IAuditlogServices, AuditlogServices>();
builder.Services.AddScoped<IConsultationRepository, ConsultationRepository>();
builder.Services.AddScoped<IConsultationServices, ConsultationServices>();
builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<ILaboratoryServices, LaboratoryServices>();
builder.Services.AddScoped<IMedicationRepository, MedicationRepository>();
builder.Services.AddScoped<IMedicationServices, MedicationServices>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IPrescriptionServices, PrescriptionServices>();
builder.Services.AddScoped<IPatientServices, PatientServices>();
builder.Services.AddScoped<IAppointmentServices, AppointmentServices>();
builder.Services.AddScoped<ICatalogServices, CatalogServices>();

// Missing Registrations
builder.Services.AddScoped<IAuditlogRepository, AuditlogRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ISpecialtiesRepository, SpecialtiesRepository>();
builder.Services.AddScoped<IEmployesRepository, EmployesRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IEmployesServices, EmployesServices>();
builder.Services.AddScoped<IRoleService, RoleServices>();
builder.Services.AddScoped<ISpecialtiesServices, SpecialtiesServices>();
builder.Services.AddScoped<IGenericRepository<Appointment>, AppointmentRepository>();
builder.Services.AddScoped<IGenericRepository<Employee>, EmployesRepository>();
builder.Services.AddScoped<IGenericRepository<Consultation>, ConsultationRepository>();
builder.Services.AddScoped<IGenericRepository<Exam>, ExamRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExamTypeServices, ExamTypeServices>();

// Schedules
builder.Services.AddScoped<IGenericRepository<ClinicSchedule>, GenericRepository<ClinicSchedule>>();
builder.Services.AddScoped<
    IGenericRepository<EmployeeSchedule>,
    GenericRepository<EmployeeSchedule>
>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();

builder.Services.Configure<Application.Models.MailSettings>(
    builder.Configuration.GetSection("EMAIL_SETTINGS")
);
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddMemoryCache();

// Supabase Configuration
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];
if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
{
    var options = new Supabase.SupabaseOptions { AutoConnectRealtime = false };
    builder.Services.AddSingleton<Supabase.Client>(_ => new Supabase.Client(
        supabaseUrl,
        supabaseKey,
        options
    ));
    builder.Services.AddScoped<ISupabaseService, SupabaseService>();
}
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddHttpContextAccessor();

// Registro del servicio de automapper
// Registrar AutoMapper: en v14 escanear ensamblados en vez de pasar un Type
builder.Services.AddAutoMapper(cfg => { }, AppDomain.CurrentDomain.GetAssemblies());

// Registrar validadores de FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>(); // Escanea el ensamblado donde se encuentra LoginDtoValidator y registra automáticamente todos los validadores que encuentre.

// Desregistrar los validadores de citas de la validación automática por contener reglas MustAsync
var createValidatorDescriptor = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<Application.DTOs.Appointment.AppointmentCreateDto>));
if (createValidatorDescriptor != null)
{
    builder.Services.Remove(createValidatorDescriptor);
}
var updateValidatorDescriptor = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IValidator<Application.DTOs.Appointment.AppointmentUpdateDto>));
if (updateValidatorDescriptor != null)
{
    builder.Services.Remove(updateValidatorDescriptor);
}

// Registrar de forma concreta para inyección en el servicio
builder.Services.AddScoped<Application.Validators.Appointment.AppointmentCreateDtoValidator>();
builder.Services.AddScoped<Application.Validators.Appointment.AppointmentUpdateDtoValidator>();

//builder.Services.AddTransient<SignInManager<User>>();
//builder.Services.AddTransient<IUserStore<User>, UserStore>();

// configuracion de Identity
builder
    .Services.AddIdentity<User, Role>(options =>
    {
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    })
    //.AddErrorDescriber<>()
    .AddDefaultTokenProviders() // // Esto es necesario para que Identity pueda generar tokens de autenticacion, como los de restablecimiento de contrasea o verificaci�n de correo electr�nico.
    .AddUserStore<UserStore>()
    .AddRoleStore<RoleStore>()
    .AddSignInManager();

// Configuracion de la autenticacion con JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtKey = jwtSettings["Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT signing key is not configured in JwtSettings:Key.");
}
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // Default: Bearer para auth
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // Default: Bearer para 401
    })
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero,
        };
        opciones.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                Console.WriteLine("¡JWT Message Received! (Bearer detectado)");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Token invalido: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token valido");

                var userIdClaim =
                    context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.Principal?.FindFirst("sub")?.Value;

                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    var cache =
                        context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                    string cacheKey = $"user-permissions-updated:{userIdClaim}";

                    if (cache.TryGetValue<DateTime>(cacheKey, out var lastUpdatedDate))
                    {
                        DateTime? tokenIssuedAt = null;

                        if (context.SecurityToken is JwtSecurityToken jwtToken)
                        {
                            tokenIssuedAt = jwtToken.ValidFrom;
                        }
                        else if (context.SecurityToken is Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jsonWebToken)
                        {
                            tokenIssuedAt = jsonWebToken.ValidFrom;
                        }

                        if (tokenIssuedAt.HasValue && tokenIssuedAt.Value != DateTime.MinValue && tokenIssuedAt.Value < lastUpdatedDate)
                        {
                            Console.WriteLine(
                                $"Token revocado para el usuario {userIdClaim} porque sus permisos cambiaron en {lastUpdatedDate} y el token es de {tokenIssuedAt.Value}"
                            );
                            context.Fail(
                                "Los permisos del usuario han sido actualizados. Por favor inicie sesión nuevamente."
                            );
                        }
                    }
                }

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine("No se proporciono token o auth requerida");
                return Task.CompletedTask;
            },
        };
    });

// Agregar servicio de autorización

builder
    .Services.AddAuthorizationBuilder()
    .AddPolicy("RequireDashboardView", policy => policy.RequireClaim("view", "Dashboard"))
    .AddPolicy(
        "RequirePatientsView",
        policy =>
            policy.RequireClaim(
                "view",
                "Pacientes - Buscar",
                "Pacientes - Registrar",
                "Pacientes - Historial de Paciente"
            )
    )
    .AddPolicy(
        "RequireBillingView",
        policy =>
            policy.RequireClaim(
                "view",
                "Facturación - Factura",
                "Facturación - Pagos",
                "Facturación - Cierre de caja",
                "Facturación - Promociones"
            )
    )
    .AddPolicy(
        "RequireConsultationsView",
        policy =>
            policy.RequireClaim(
                "view",
                "Citas - Hoy",
                "Citas - Agendar",
                "Citas - Disponibilidad",
                "Consultas - Activa",
                "Consultas - Crear",
                "Consultas - Historial"
            )
    )
    .AddPolicy(
        "RequireLabView",
        policy =>
            policy.RequireClaim(
                "view",
                "Laboratorio - Registrar Resultados",
                "Laboratorio - Historial",
                "Laboratorio - Catálogo de Exámenes"
            )
    )
    .AddPolicy(
        "RequireHRView",
        policy =>
            policy.RequireClaim(
                "view",
                "Recursos Humanos - Empleados",
                "Recursos Humanos - Asistencia",
                "Recursos Humanos - Especialidades",
                "Recursos Humanos - Cargos"
            )
    )
    .AddPolicy("RequireReportsView", policy => policy.RequireClaim("view", "Reportes"))
    .AddPolicy(
        "RequireAdministrationView",
        policy =>
            policy.RequireClaim(
                "view",
                "Administración - Usuarios",
                "Administración - Horarios",
                "Administración - Auditoría",
                "Administración - Archivos Digitales"
            )
    );
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection(); // Ahora va después de UseCors

app.UseCors("AllowFrontend"); // Mover aquí

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();


//app.Use(async (context, next) =>
//{
//    Console.WriteLine($"Request incoming: {context.Request.Method} {context.Request.Path}");
//    Console.WriteLine($"Authorization header: {context.Request.Headers["Authorization"].FirstOrDefault() ?? "NO HEADER"}");
//    await next();
//});
