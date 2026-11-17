using System.ComponentModel.DataAnnotations;
using Ewallet.Models;

namespace Ewallet.Dtos.Users;

public record CreateUserRequest(
    [Required, StringLength(50, MinimumLength = 3), RegularExpression("^[a-zA-Z0-9_.]+$", ErrorMessage = "Username hanya boleh huruf, angka, titik, dan underscore.")] string Username,
    [Required, StringLength(100)] string FullName,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, Phone, StringLength(20)] string Phone
);

public record UpdateUserRequest(
    [Required, StringLength(100)] string FullName,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, Phone, StringLength(20)] string Phone
);

public record UserResponse(
    long Id,
    string Username,
    string FullName,
    string Email,
    string Phone,
    UserStatus Status,
    long? WalletId,
    string? WalletNumber,
    decimal Balance);