#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

// Script para generar hash de password con BCrypt
// Uso: dotnet script generate-password-hash.csx <password>
// Ejemplo: dotnet script generate-password-hash.csx admin123

if (Args.Count == 0)
{
    Console.WriteLine("❌ Error: Debes proporcionar un password");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine("  dotnet script generate-password-hash.csx <password>");
    Console.WriteLine();
    Console.WriteLine("Ejemplo:");
    Console.WriteLine("  dotnet script generate-password-hash.csx admin123");
    Console.WriteLine();
    return;
}

var password = Args[0];
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

Console.WriteLine();
Console.WriteLine("✅ Password hash generado:");
Console.WriteLine();
Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash:     {hash}");
Console.WriteLine();
Console.WriteLine("Para usar este hash en la base de datos:");
Console.WriteLine($"PasswordHash = \"{hash}\"");
Console.WriteLine();
