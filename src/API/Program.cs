
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Store; 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


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

builder.Services.AddHttpContextAccessor();

//builder.Services.AddTransient<SignInManager<User>>();
//builder.Services.AddTransient<IUserStore<User>, UserStore>();

// configuracion de Identity
builder.Services.AddIdentity<User , Role>(options =>
{
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Valida si el emisor del token es quien esperamos.
            ValidateAudience = true, // Valida si el destinatario del token es la audiencia correcta 
            ValidateLifetime = true, //  Valida la fecha de expiración del token.
            ValidateIssuerSigningKey = true, // Valida la firma del token usando la clave secreta. Esto asegura que el token no ha sido alterado.
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = key
        };
    });


// Agregar servicio de autorización

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Habilitar los middlewares de autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
