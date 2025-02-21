using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IUserRepository, UserRepository>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/users", (IUserRepository repo) => repo.GetAllUsers());

app.MapGet("/users/{id}", (int id, IUserRepository repo) => 
{
    var user = repo.GetUserById(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/users", (User user, IUserRepository repo) => 
{
    repo.AddUser(user);
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", (int id, User updatedUser, IUserRepository repo) => 
{
    var user = repo.GetUserById(id);
    if (user is null) return Results.NotFound();

    repo.UpdateUser(id, updatedUser);
    return Results.NoContent();
});

app.MapDelete("/users/{id}", (int id, IUserRepository repo) => 
{
    var user = repo.GetUserById(id);
    if (user is null) return Results.NotFound();

    repo.DeleteUser(id);
    return Results.NoContent();
});

app.Run();

public record User(int Id, string Name, string Email);

public interface IUserRepository
{
    IEnumerable<User> GetAllUsers();
    User GetUserById(int id);
    void AddUser(User user);
    void UpdateUser(int id, User updatedUser);
    void DeleteUser(int id);
}

public class UserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public IEnumerable<User> GetAllUsers() => _users;

    public User GetUserById(int id)
    {
        return _users.Find(user => user.Id == id)!;
    }

    public void AddUser(User user) => _users.Add(user);

    public void UpdateUser(int id, User updatedUser)
    {
        var index = _users.FindIndex(user => user.Id == id);
        if (index != -1) _users[index] = updatedUser;
    }

    public void DeleteUser(int id) => _users.RemoveAll(user => user.Id == id);
}
