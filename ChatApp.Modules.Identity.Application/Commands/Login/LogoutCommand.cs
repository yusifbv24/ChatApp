using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Identity.Application.Commands.Login
{
    public record LogoutCommand(
        Guid UserId
    ):IRequest<Result>;


    public class LogoutCommandHandler(IUnitOfWork unifOfWork) : IRequestHandler<LogoutCommand, Result>
    {
        public async Task<Result> Handle(
            LogoutCommand request,
            CancellationToken cancellationToken)
        {
            // Find all active refresh token for this User
            var tokens = await unifOfWork.RefreshTokens
                .Where(rt => rt.UserId == request.UserId)
                .ToListAsync(cancellationToken);

            // Revoke each token
            foreach(var token in tokens)
            {
                token.Revoke();
                unifOfWork.RefreshTokens.Update(token);
            }
            await unifOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}