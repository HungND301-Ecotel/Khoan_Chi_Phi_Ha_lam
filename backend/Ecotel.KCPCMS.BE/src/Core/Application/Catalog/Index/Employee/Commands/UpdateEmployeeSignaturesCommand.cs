using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Employee;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Commands;

public record UpdateEmployeeSignatureCommand(UpdateEmployeeSignaturesDto UpdateModel) : IRequest<bool>;

public class UpdateEmployeeSignatureCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher)
    : IRequestHandler<UpdateEmployeeSignatureCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();
    private readonly IWriteRepository<Domain.Entities.Identity.UserSignature> _userSignatureRepository = unitOfWork.GetRepository<Domain.Entities.Identity.UserSignature>();

    public async Task<bool> Handle(UpdateEmployeeSignatureCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.UpdateModel.EmployeeId,
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            var existing = await _userSignatureRepository.GetFirstOrDefaultAsync(
                predicate: s => s.UserId == employee.UserId
                             && s.SignatureType == request.UpdateModel.SignatureType
                             && s.IsActive,
                disableTracking: false);

            if (existing is not null)
            {
                existing.Deactivate();
                _userSignatureRepository.Update(existing);
            }

            UserSignature newSignature;

            if (request.UpdateModel.SignatureType == SignatureType.Digital)
            {
                var pinHash = string.IsNullOrWhiteSpace(request.UpdateModel.PinHash)
                    ? null
                    : passwordHasher.HashPassword(null!, request.UpdateModel.PinHash);

                newSignature = UserSignature.CreateForDigital(
                    employee.UserId,
                    request.UpdateModel.CertificateId,
                    pinHash,
                    request.UpdateModel.IsPinSaved);
            }
            else
            {
                newSignature = UserSignature.Create(
                    employee.UserId,
                    request.UpdateModel.SignatureType,
                    request.UpdateModel.SignatureFileUrl);
            }

            await _userSignatureRepository.InsertAsync(newSignature, cancellationToken);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}