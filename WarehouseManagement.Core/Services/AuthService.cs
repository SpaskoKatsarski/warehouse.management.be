﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WarehouseManagement.Core.Contracts;
using WarehouseManagement.Core.DTOs.Auth;
using WarehouseManagement.Infrastructure.Data.Common;
using WarehouseManagement.Infrastructure.Data.Models;
using static WarehouseManagement.Common.MessageConstants.Keys.AuthMassegeKeys;

public class AuthService : IAuthService
{
    private readonly IRepository repository;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IConfiguration configuration;

    public AuthService(IRepository repository, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        this.repository = repository;
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.configuration = configuration;
    }

    public string GenerateJwtToken(string userId, string username, string email)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]!);

        var token = ComposeAccessToken(userId, userId, email);

        return token;
    }

    public async Task<string> GenerateRefreshToken(string userId)
    {
        var refreshToken = new RefreshToken()
        {
            Token = Guid.NewGuid().ToString().Replace("-", ""),
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            UserId = Guid.Parse(userId)
        };

        await repository.AddAsync(refreshToken);
        await repository.SaveChangesAsync();

        return refreshToken.Token;
    }

    public async Task<string> GenerateAccessTokenFromRefreshToken(string refreshToken)
    {
        var refreshTokenEntity = await repository
            .All<RefreshToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (refreshTokenEntity == null)
        {
            throw new KeyNotFoundException(RefreshTokenNotFound);
        }

        if (refreshTokenEntity.ExpirationDate < DateTime.UtcNow)
        {
            throw new InvalidOperationException(RefreshTokenHasExpired);
        }

        var token = ComposeAccessToken(refreshTokenEntity.UserId.ToString(), refreshTokenEntity.User.UserName!, refreshTokenEntity.User.Email!);

        return token;
    }

    private string ComposeAccessToken(string userId, string username, string email)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email)
            }),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task RegisterAsync(RegisterDto registerDto)
    {
        var user = new ApplicationUser
        {
            UserName = registerDto.Username,
            Email = registerDto.Email,
        };

        var result = await userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<string> LoginAsync(LoginDto loginDto)
    {
        var user = await repository
            .All<ApplicationUser>()
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.UserName == loginDto.Username);

        if (user == null)
        {
            throw new ArgumentException("Email or password is incorrect.");
        }

        var result = await signInManager.PasswordSignInAsync(user.UserName!, loginDto.Password, true, false);

        if (!result.Succeeded)
        {
            throw new Exception("Invalid login attempt.");
        }

        await RevokeOldRefreshTokens(user);
        return GenerateJwtToken(user!.Id.ToString(), user.UserName!, user.Email!);
    }

    private async Task RevokeOldRefreshTokens(ApplicationUser user)
    {
        foreach (var refreshToken in user.RefreshTokens)
        {
            // Maybe directly delete them from the DB
            if (!refreshToken.IsRevoked)
            {
                refreshToken.IsRevoked = true;
            }
        }

        await repository.SaveChangesAsync();
    }

    public async Task LogoutAsync(string userId)
    {
        var refreshToken = await repository
            .All<RefreshToken>()
            .FirstAsync(t => t.UserId.ToString() == userId);

        refreshToken.IsRevoked = true;

        await signInManager.SignOutAsync();
    }
}