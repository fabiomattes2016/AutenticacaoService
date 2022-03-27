using System.Security.Claims;
using System.Text;
using AutenticacaoService;
using AutenticacaoService.Data;
using AutenticacaoService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiniValidation;

const string VERSION = "v1";
const string BASE_ENDPOINT = $"/api/{VERSION}";

var key = Encoding.ASCII.GetBytes(Settings.Secret);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AutenticacaoDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        Description = "Autenticação Bearer com token JWT",
        Type = SecuritySchemeType.Http
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                },
            },
            new List<string>()
        }
    });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UsePathBase("/swagger/index.html");

app.UseCors(x => x.AllowAnyHeader()
      .AllowAnyMethod()
      .WithOrigins("*"));

app.UseAuthentication();
app.UseAuthorization();

#region CRUD
app.MapGet($"{BASE_ENDPOINT}/users", async (AutenticacaoDbContext context) =>
{
    var users = await context.Users.ToListAsync();

    List<object> response = new List<object>();

    foreach (var user in users)
    {
        var data = new
        {
            Id = user.Id,
            Name = user.Name,
            Username = user.Username,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        response.Add(data);
    }


    return Results.Ok(response);
})
.RequireAuthorization()
.Produces<User>();

app.MapGet($"{BASE_ENDPOINT}/users" + "/{id}", async (Guid id, AutenticacaoDbContext context, ClaimsPrincipal claim) =>
{
    var user = await context.Users.FindAsync(id);

    if (user == null)
        return Results.NotFound(new { message = "Usuário não encontrado" });

    var response = new
    {
        Id = user.Id,
        Name = user.Name,
        Username = user.Username,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    return Results.Ok(response);
})
.RequireAuthorization()
.Produces<User>();

app.MapPost($"{BASE_ENDPOINT}/users", async (User user, AutenticacaoDbContext context, ClaimsPrincipal claim) =>
{
    MiniValidator.TryValidate(user, out var errors);

    var userDb = await context.Users.FirstOrDefaultAsync(x => x.Username == user.Username);

    if (userDb != null)
        return Results.BadRequest(new { Message = "Usuário já cadastrado" });

    var passwordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);

    user.Password = passwordHash;
    user.CreatedAt = DateTime.UtcNow;

    if (errors.Count > 0)
        return Results.BadRequest(errors);
    else
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var response = new
        {
            Id = user.Id,
            Name = user.Name,
            Username = user.Username,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Results.Created($"{BASE_ENDPOINT}/users/{user.Id}", response);
    }
}).Produces<User>(201);

app.MapPut($"{BASE_ENDPOINT}/users", async (User user, AutenticacaoDbContext context, ClaimsPrincipal claim) =>
{
    var dbUser = await context.Users.FindAsync(user.Id);

    if (dbUser == null)
    {
        return Results.NotFound(new { message = "Usuário não encontrado" });
    }

    dbUser.Name = user.Name;
    dbUser.Username = user.Username;
    dbUser.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return Results.Ok(dbUser);
})
.RequireAuthorization()
.Produces<User>(200);

app.MapDelete($"{BASE_ENDPOINT}/users/" + "{id}", async (Guid id, AutenticacaoDbContext context, ClaimsPrincipal claim) =>
{
    var dbUser = await context.Users.FindAsync(id);

    if (dbUser == null)
    {
        return Results.NotFound(new { message = "Usuário não encontrado" });
    }

    context.Users.Remove(dbUser);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization()
.Produces(204);

app.MapPut($"{BASE_ENDPOINT}/users/password/" + "{id}", async (Guid id, User user, AutenticacaoDbContext context, ClaimsPrincipal claim) =>
{
    var dbUser = await context.Users.FindAsync(id);
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);

    if (dbUser == null)
    {
        return Results.NotFound(new { message = "Usuário não encontrado" });
    }

    dbUser.Password = passwordHash;
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization()
.Produces(200);
#endregion

#region Autenticacao
app.MapPost($"{BASE_ENDPOINT}/users/login", async (User user, AutenticacaoDbContext context) =>
{
    var dbUser = await context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);

    if (dbUser == null)
        return Results.NotFound(new { message = "Credenciais inválidas" });

    var validPassword = BCrypt.Net.BCrypt.Verify(user.Password, dbUser.Password);

    if (!validPassword)
        return Results.NotFound(new { message = "Credenciais inválidas" });

    var token = TokenService.GenerateToken(dbUser);

    return Results.Ok(new
    {
        Id = dbUser.Id,
        Name = dbUser.Name,
        UserName = dbUser.Username,
        AccessToken = token
    });


});
#endregion

app.Run();
