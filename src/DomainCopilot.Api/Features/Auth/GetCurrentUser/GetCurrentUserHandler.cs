using System.Threading;
using System.Threading.Tasks;
using DomainCopilot.Api.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace DomainCopilot.Api.Features.Auth.GetCurrentUser;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, IResult>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetCurrentUserHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IResult> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return Results.NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        var response = new UserProfileResponse(
            Id: user.Id.ToString(),
            Email: user.Email ?? string.Empty,
            FullName: user.FullName,
            Department: user.Department,
            Roles: roles
        );

        return Results.Ok(response);
    }
}
