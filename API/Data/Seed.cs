using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using API.Entities;
using API.DTOs;
using API.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace API.Data;

public class Seed
{
    public static async Task SeedUsers(AppDbContext context, IPasswordHasherService passwordHasher)
    {
        if (await context.Users.AnyAsync()) return;

        var memberData = await File.ReadAllTextAsync("Data/UserSeedData.json");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var members = JsonSerializer.Deserialize<List<SeedUserDto>>(memberData, options);

        if (members == null)
        {
            Console.WriteLine("No members in seed data");
            return;
        }

        foreach (var member in members)
        {
            var user = new AppUser
            {
                Id = member.Id,
                Email = member.Email,
                DisplayName = member.DisplayName,
                ImageUrl = member.ImageUrl,
                PasswordHash = passwordHasher.HashPassword("Password"),
                Member = new Member
                {
                    Id = member.Id,
                    DisplayName = member.DisplayName,
                    Description = member.Description,
                    DateofBirth = member.DateofBirth,
                    ImageUrl = member.ImageUrl,
                    Gender = member.Gender,
                    City = member.City,
                    Country = member.Country,
                    Created = DateTime.SpecifyKind(member.Created, DateTimeKind.Utc),
                    LastActive = DateTime.SpecifyKind(member.LastActive, DateTimeKind.Utc),
                }
            };

            user.Member.Photos.Add(new Photo
            {
                Url = member.ImageUrl!,
                MemberId = member.Id
            });

            context.Users.Add(user);
        }

        await context.SaveChangesAsync();
    }
}
