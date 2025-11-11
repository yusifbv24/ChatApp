using ChatApp.Modules.Identity.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Modules.Identity.Application.Commands.Login
{
    public record LogoutCommand(
        Guid UserId
    ):IRequest<Result>;


    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public LogoutCommandHandler(IUnitOfWork unifOfWork)
        {
            _unitOfWork = unifOfWork;
        }



        public async Task<Result> Handle(
            LogoutCommand request,
            CancellationToken cancellationToken)
        {
            // Find all active refresh token for this User
            var tokens = await _unitOfWork.RefreshTokens
                .Where(rt => rt.UserId == request.UserId)
                .ToListAsync(cancellationToken);

            // Revoke each token
            foreach(var token in tokens)
            {
                token.Revoke();
                _unitOfWork.RefreshTokens.Update(token);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}