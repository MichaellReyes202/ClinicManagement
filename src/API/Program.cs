
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
using Infrastructure.Store;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

// Area para agregar los filtros personalizados 
builder.Services.AddScoped<AuthorizationAuditFilter>();

// Agregar servicios al contenedor
builder.Services.AddControllers(options =>
{
    // Esto asegura que el filtro se ejecute en todas las peticiones
    options.Filters.Add(typeof(AuthorizationAuditFilter));
})
.AddJsonOptions(options =>
{
    // Ignora los ciclos de referencia
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

// Configurar DbContext con PostgreSQL
builder.Services.AddDbContext<ClinicDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var validationErrors = context.ModelState
           .Where(ms => ms.Value?.Errors.Count > 0)
           .SelectMany(kvp => kvp.Value!.Errors.Select(e =>
               new ValidationError(kvp.Key, e.ErrorMessage)))
           .ToList();
        var result = Result.Failure(validationErrors);

        return new BadRequestObjectResult(new
        {
            message = "Validation failed - check required fields",
            errors = result.ValidationErrors.Select(v => new
            {
                propertyName = v.PropertyName,
                errorMessage = v.ErrorMessage
            })
        });
    };
});

// ======================================================
// 1. Añadir el servicio CORS
// ======================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5174", "http://localhost:5173") 
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // si usas cookies o tokens Bearer
    });
});


// Area para registrar los servicios 
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleService, RoleServices>();   
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployesServices, EmployesServices>();
builder.Services.AddScoped<ISpecialtiesServices, SpecialtiesServices>();
builder.Services.AddScoped<ICatalogServices, CatalogServices>();
builder.Services.AddScoped<IPatientServices, PatientServices>();
builder.Services.AddScoped<IExamTypeServices ,  ExamTypeServices>();
builder.Services.AddScoped<IAppointmentServices , AppointmentServices>();
builder.Services.AddScoped<IAuditlogRepository, AuditlogRepository>();

builder.Services.AddScoped<ISpecialtiesRepository, SpecialtiesRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEmployesRepository, EmployesRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IPositionServices, PositionServices>();
builder.Services.AddScoped<ICatBloodRepository, CatBloodRepository>();
builder.Services.AddScoped<ISexRepository, SexRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IExamTypeRepository , ExamTypeRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<ICatAppointmentStatusRepository, CatAppointmentStatusRepository>();
builder.Services.AddScoped<IAuditlogServices, AuditlogServices>();
builder.Services.AddScoped<IConsultationRepository, ConsultationRepository>();
builder.Services.AddScoped<IConsultationServices, ConsultationServices>();
builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<ILaboratoryServices, LaboratoryServices>();





builder.Services.AddHttpContextAccessor();


// Registro del servicio de automapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

// Registrar validadores de FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>(); // Escanea el ensamblado donde se encuentra LoginDtoValidator y registra automáticamente todos los validadores que encuentre.


//builder.Services.AddTransient<SignInManager<User>>();
//builder.Services.AddTransient<IUserStore<User>, UserStore>();

// configuracion de Identity
builder.Services.AddIdentity<User, Role>(options =>
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
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
})
//.AddErrorDescriber<>()
.AddDefaultTokenProviders() // // Esto es necesario para que Identity pueda generar tokens de autenticacion, como los de restablecimiento de contrasea o verificaci�n de correo electr�nico.
.AddUserStore<UserStore>()
.AddRoleStore<RoleStore>()
.AddSignInManager();

// Configuracion de la autenticacion con JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

builder.Services
    .AddAuthentication(options =>  
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;  // Default: Bearer para auth
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;     // Default: Bearer para 401
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
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine("No se proporciono token o auth requerida");
                return Task.CompletedTask;
            }
        };
    });

// Agregar servicio de autorización

builder.Services.AddAuthorizationBuilder()
    // Agregar servicio de autorización
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
    // Agregar servicio de autorización
    .AddPolicy("UserOnly", policy => policy.RequireRole("User"))
    // Agregar servicio de autorización
    .AddPolicy("AdminOrUser", policy => policy.RequireRole("Admin", "User"));

builder.Services.AddOpenApi();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection(); // Ahora va después de UseCors

app.UseCors("AllowFrontend"); // Mover aquí


//app.Use(async (context, next) =>
//{
//    Console.WriteLine($"Request incoming: {context.Request.Method} {context.Request.Path}");
//    Console.WriteLine($"Authorization header: {context.Request.Headers["Authorization"].FirstOrDefault() ?? "NO HEADER"}");
//    await next();
//});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();




//app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
//{
//    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
//    var exception = exceptionHandlerFeature?.Error!;

//    var error = new Error()
//    {
//        Message = exception.Message,
//        StackTrace = exception.StackTrace,
//        Fechas = DateTime.UtcNow
//    };
//    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
//    dbContext.Add(error);
//    await dbContext.SaveChangesAsync();
//    await Results.InternalServerError(new
//    {
//        tipo = "error",
//        mensaje = "Ha ocurrido un error inisperado",
//        estatus = 500,
//    }).ExecuteAsync(context);
//}));

//namespace BibliotecaAPI.Entidades
//{
//    public class Error
//    {
//        public Guid Id { get; set; }
//        public required string Message { get; set; }
//        public string? StackTrace { get; set; }
//        public DateTime? Fechas { get; set; }
//    }
//}
