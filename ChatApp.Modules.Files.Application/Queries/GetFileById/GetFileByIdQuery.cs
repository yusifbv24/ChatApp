using ChatApp.Modules.Files.Application.DTOs.Responses;
using ChatApp.Modules.Files.Application.Interfaces;
using ChatApp.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Modules.Files.Application.Queries.GetFileById
{
    public record GetFileByIdQuery(
        Guid FileId,
        Guid RequestedBy
    ):IRequest<Result<FileDto?>>;



    public class GetFileByIdQueryHandler : IRequestHandler<GetFileByIdQuery, Result<FileDto?>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetFileByIdQueryHandler> _logger;

        public GetFileByIdQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetFileByIdQueryHandler> logger)
        {
            _unitOfWork= unitOfWork;
            _logger= logger;
        }


        public async Task<Result<FileDto?>> Handle(
            GetFileByIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var fileDto = await _unitOfWork.Files.GetFileDtoByIdAsync(
                    request.FileId,
                    cancellationToken);

                if (fileDto == null)
                    return Result.Success<FileDto?>(null);

                // Only uploader can view file metadata
                // In production, you might want to allow viewing if file is shared in a channel/conversation
                if (fileDto.UploadedBy != request.RequestedBy)
                {
                    return Result.Failure<FileDto?>("You don't have permission to view the file");
                }

                return Result.Success<FileDto?>(fileDto);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving file {FileId}", request.FileId);
                return Result.Failure<FileDto?>("An error occurred while retrieving the file");
            }
        }
    }
}