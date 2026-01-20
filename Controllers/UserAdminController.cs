using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MikadoNet.Models;

namespace MikadoNet.Controllers;

[Route("admin/users")]
public class UserAdminController : Controller
{
    private readonly UserManager<AppUser> _userManager;

    public UserAdminController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var users = _userManager.Users
            .Select(user => new UserListItem
            {
                Id = user.Id,
                UserName = user.UserName,
                TantoCode = user.TantoCode
            })
            .ToList();

        return View(users);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new UserCreateViewModel());
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser
        {
            UserName = model.UserName,
            TantoCode = model.TantoCode
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var model = new UserEditViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            TantoCode = user.TantoCode
        };

        return View(model);
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.UserName = model.UserName;
        user.TantoCode = model.TantoCode;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
        }

        return RedirectToAction(nameof(Index));
    }
}
