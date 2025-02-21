using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Diagnostics;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Collections.Generic;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();
builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();

// Use the request and response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// Global exception handling middleware
app.UseExceptionHandler("/error");

app.MapGet("/", () => "Root");

app.MapGet("/users", async (IUserRepository repo, int page = 1, int pageSize = 10) =>
{
    try
    {
        var users = await repo.GetAllUsersAsync(page, pageSize);
        return Results.Ok(users);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapGet("/users/{id}", async (int id, IUserRepository repo) => 
{
    try
    {
        var user = await repo.GetUserByIdAsync(id);
        return user is not null ? Results.Ok(user) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/users", async (User user, IUserRepository repo, IValidator<User> validator) => 
{
    try
    {
        var validationResult = await validator.ValidateAsync(user);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }
        await repo.AddUserAsync(user);
        return Results.Created($"/users/{user.Id}", user);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPut("/users/{id}", async (int id, User updatedUser, IUserRepository repo, IValidator<User> validator) => 
{
    try
    {
        var user = await repo.GetUserByIdAsync(id);
        if (user is null) return Results.NotFound();

        var validationResult = await validator.ValidateAsync(updatedUser);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }

        await repo.UpdateUserAsync(id, updatedUser);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapDelete("/users/{id}", async (int id, IUserRepository repo) => 
{
    try
    {
        var user = await repo.GetUserByIdAsync(id);
        if (user is null) return Results.NotFound();
        await repo.DeleteUserAsync(id);
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.Map("/error", (HttpContext context) =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    return Results.Problem(detail: exception?.Message, statusCode: 500);
});

app.Run();

public record User(int Id, string Name, string Email);

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllUsersAsync(int page, int pageSize);
    Task<User?> GetUserByIdAsync(int id);
    Task AddUserAsync(User user);
    Task UpdateUserAsync(int id, User updatedUser);
    Task DeleteUserAsync(int id);
}

public class UserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task<IEnumerable<User>> GetAllUsersAsync(int page, int pageSize)
    {
        var users = _users.Skip((page - 1) * pageSize).Take(pageSize);
        return Task.FromResult(users.AsEnumerable());
    }

    public Task<User?> GetUserByIdAsync(int id)
    {
        var user = _users.Find(user => user.Id == id);
        return Task.FromResult(user);
    }

    public Task AddUserAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(int id, User updatedUser)
    {
        var index = _users.FindIndex(user => user.Id == id);
        if (index != -1) _users[index] = updatedUser;
        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(int id)
    {
        _users.RemoveAll(user => user.Id == id);
        return Task.CompletedTask;
    }
}

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(user => user.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(user => user.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
    }
}
