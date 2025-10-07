
using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Store;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllers();

// Configurar DbContext con PostgreSQL
builder.Services.AddDbContext<ClinicDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});


// Area para registrar los servicios 
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoleService, RoleServices>();   
builder.Services.AddScoped<IUserService, UserService>();


builder.Services.AddScoped<ISpecialtiesServices, SpecialtiesServices>();
builder.Services.AddScoped<ISpecialtiesRepository, SpecialtiesRepository>();
builder.Services.AddScoped<IEmployesRepository, EmployesRepository>();

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
})
//.AddErrorDescriber<>()
.AddDefaultTokenProviders() // // Esto es necesario para que Identity pueda generar tokens de autenticaci�n, como los de restablecimiento de contrase�a o verificaci�n de correo electr�nico.
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
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    Console.WriteLine($"Request incoming: {context.Request.Method} {context.Request.Path}");
    Console.WriteLine($"Authorization header: {context.Request.Headers["Authorization"].FirstOrDefault() ?? "NO HEADER"}");
    await next();
});

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
