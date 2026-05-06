using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Department;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Department.Queries;

public record GetDepartmentByIdQuery(DefaultIdType Id) : IRequest<DepartmentDto>;

public class GetDepartmentByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.Department> _departmentRepository = unitOfWork.GetRepository<Domain.Entities.Index.Department>();

    public async Task<DepartmentDto> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: q => q.Include(d => d.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code?.Value ?? string.Empty,
            Name = department.Name
        };
    }
}
