﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WarehouseManagement.Core.Contracts;
using WarehouseManagement.Infrastructure.Data.Common;
using WarehouseManagement.Infrastructure.Data.Models;
using static WarehouseManagement.Common.MessageConstants.Keys.AuthMassegeKeys;

namespace WarehouseManagement.Core.Services;

public class JwtService : IJwtService
{
    private readonly IRepository repository;
    private readonly IConfiguration configuration;

    public JwtService(IRepository repository, IConfiguration configuration)
    {
        this.repository = repository;
        this.configuration = configuration;
    }

    public string ComposeAccessToken(string userId, string username, string email)
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

    public async Task RevokeOldRefreshTokens(ApplicationUser user)
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
}
