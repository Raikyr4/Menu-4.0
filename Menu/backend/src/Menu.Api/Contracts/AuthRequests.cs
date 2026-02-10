namespace Menu.Api.Contracts;

public sealed record LoginRequest(string Username, string Password);
public sealed record RegisterRequest(string Username, string Password, string Nome);
