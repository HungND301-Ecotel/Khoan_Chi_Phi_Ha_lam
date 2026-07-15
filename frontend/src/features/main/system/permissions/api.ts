import { api } from '@/lib/api';

export interface PermissionItemDto {
	id: string;
	code: number;
	name: string;
}

export interface SubModuleCatalogDto {
	id: string;
	name: string;
	code: string;
	sortOrder: number;
	allowedPermissions: number[];
}

export interface ModuleCatalogDto {
	id: string;
	name: string;
	code: string;
	sortOrder: number;
	subModules: SubModuleCatalogDto[];
}

export interface PermissionCatalogDto {
	modules: ModuleCatalogDto[];
	globalPermissions: PermissionItemDto[];
}

// Department
export interface DepartmentModulePermissionInputDto {
	moduleId: string;
	permissionId: string;
	isGranted: boolean;
}

export interface UpdateDepartmentPermissionsDto {
	departmentId: string;
	permissions: DepartmentModulePermissionInputDto[];
}

// Position
export interface PositionSubmodulePermissionInputDto {
	subModuleId: string;
	permissionId: string;
	isGranted: boolean;
}

export interface UpdatePositionPermissionsDto {
	positionId: number;
	permissions: PositionSubmodulePermissionInputDto[];
}

// User Override
export interface UserPermissionOverrideInputDto {
	subModuleId: string;
	permissionId: string;
	isGranted: boolean;
	reason?: string;
}

export interface UpdateUserOverridePermissionsDto {
	userId: number;
	overrides: UserPermissionOverrideInputDto[];
}

// --- API Functions ---

export const getPermissionCatalog = () => {
	return api.get<PermissionCatalogDto>('/v1/system/permission/catalog');
};

export const getDepartmentPermissions = (departmentId: string) => {
	return api.get<UpdateDepartmentPermissionsDto>(
		`/v1/system/permission/department/${departmentId}`,
	);
};

export const updateDepartmentPermissions = (
	dto: UpdateDepartmentPermissionsDto,
) => {
	return api.post<boolean, UpdateDepartmentPermissionsDto>(
		'/v1/system/permission/department',
		dto,
	);
};

export const getPositionPermissions = (positionId: number) => {
	return api.get<UpdatePositionPermissionsDto>(
		`/v1/system/permission/position/${positionId}`,
	);
};

export const updatePositionPermissions = (
	dto: UpdatePositionPermissionsDto,
) => {
	return api.post<boolean, UpdatePositionPermissionsDto>(
		'/v1/system/permission/position',
		dto,
	);
};

export const getUserOverridePermissions = (userId: number) => {
	return api.get<UpdateUserOverridePermissionsDto>(
		`/v1/system/permission/user-override/${userId}`,
	);
};

export const updateUserOverridePermissions = (
	dto: UpdateUserOverridePermissionsDto,
) => {
	return api.post<boolean, UpdateUserOverridePermissionsDto>(
		'/v1/system/permission/user-override',
		dto,
	);
};
