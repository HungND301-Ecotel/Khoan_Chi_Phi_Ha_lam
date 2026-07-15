using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.UnitOfWork;
using Application.Dto.Authorization.Permission;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Users.Queries;

public record class GetCurrentUserPermissionQuery() : IRequest<UserPermissionsDto>;

public class GetCurrentUserPermissionQueryHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IPermissionCacheService cache)
    : IRequestHandler<GetCurrentUserPermissionQuery, UserPermissionsDto>
{
    public async Task<UserPermissionsDto> Handle(GetCurrentUserPermissionQuery request, CancellationToken cancellationToken)
    {
        int userId = currentUser.GetUserId();

        var userRepository = unitOfWork.GetRepository<User>();
        var user = await userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Id == userId,
            disableTracking: true) ?? throw new NotFoundException("Tài khoản không tồn tại");

        var employeeRepo = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();

        var employee = await employeeRepo.GetFirstOrDefaultAsync(
            predicate: e => e.UserId == userId,
            include: e => e.Include(x => x.Position).Include(x => x.Department),
            disableTracking: true)
            ?? throw new NotFoundException("Tài khoản chưa được liên kết với hồ sơ nhân viên");

        // Dùng cache → nếu miss thì UserPermissionResolver tự resolve và set cache
        var permissions = await cache.GetUserPermissionsAsync(userId, cancellationToken);

        return new UserPermissionsDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            Fullname = employee.FullName,
            EmployeeId = employee.Id,
            PositionId = employee.PositionId,
            PositionName = employee.Position?.Name,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name,
            Permissions = permissions
        };
    }
}