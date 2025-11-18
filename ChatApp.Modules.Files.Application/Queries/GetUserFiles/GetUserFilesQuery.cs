using ChatApp.Modules.Files.Application.DTOs.Responses;
using ChatApp.Modules.Files.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Files.Application.Queries.GetUserFiles
{
    public record GetUserFilesQuery(
        Guid UserId,
        int PageSize=50,
        int Skip=0
    ):IRequest<Result<List<FileDto>>>;



    public class GetUserFilesQueryHandler: IRequestHandler<GetUserFilesQuery, Result<List<FileDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetUserFilesQueryHandler> _logger;

        public GetUserFilesQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetUserFilesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<FileDto>>> Handle(
            GetUserFilesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var files = await _unitOfWork.Files.GetUserFilesAsync(
                    request.UserId,
                    request.PageSize,
                    request.Skip,
                    cancellationToken);

                return Result.Success(files);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving files for user {UserId}", request.UserId);
                return Result.Failure<List<FileDto>>("An error occurred while retrieving files");
            }
        }
    }
}