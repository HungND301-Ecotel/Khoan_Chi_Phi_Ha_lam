using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Employee;
using Domain.Entities.Identity;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Queries;

public record GetEmployeeSignatureQuery(int EmployeeId) : IRequest<List<EmployeeSignatureResponseDto>>;

public class GetEmployeeSignatureQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetEmployeeSignatureQuery, List<EmployeeSignatureResponseDto>>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Employee>();
    private readonly IWriteRepository<UserSignature> _signatureRepository =
        unitOfWork.GetRepository<UserSignature>();

    public async Task<List<EmployeeSignatureResponseDto>> Handle(
        GetEmployeeSignatureQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.EmployeeId,
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var signatures = await _signatureRepository.GetAllAsync(
            predicate: s => s.UserId == employee.UserId && s.IsActive,
            disableTracking: true);

        return signatures
            .Select(s => new EmployeeSignatureResponseDto
            {
                Id = s.Id,
                SignatureType = s.SignatureType,
                CertificateId = s.CertificateId,
                IsPinSaved = s.IsPinSaved,
                IsActive = s.IsActive
            })
            .ToList();
    }
}