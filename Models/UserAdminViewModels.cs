using System.ComponentModel.DataAnnotations;

namespace MikadoNet.Models;

public class UserListItem
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public short? TantoCode { get; set; }
}

public class UserCreateViewModel
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public short? TantoCode { get; set; }
}

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public short? TantoCode { get; set; }

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
}
