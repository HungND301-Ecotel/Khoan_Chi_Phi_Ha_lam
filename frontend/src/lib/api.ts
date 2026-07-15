export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
const base = import.meta.env.VITE_API_BASE_URL;

import { authStorage } from '@/lib/auth-storage';
import { TokenRefreshService } from '@/lib/token-refresh-service';

export type BaseResponse<T> = {
	result: T;
	success: boolean;
	message: string;
};

type ErrorResponsePayload = {
	type?: string;
	errors?: Record<string, string | string[]>;
	title?: string;
	status?: number;
	message?: string;
};

type FetchOptions = {
	requiresAuth?: boolean;
};

export class ErrorResponse {
	readonly success = false;
	type: string;
	errors: Record<string, string | string[]>;
	title: string;
	status: number;
	message: string;

	constructor(response: ErrorResponsePayload) {
		this.type = response.type ?? '';
		this.errors = response.errors ?? {};
		this.title = response.title ?? '';
		this.status = response.status ?? 0;
		this.message = response.message ?? '';
	}
}

export type PaggingResponse<T> = {
	data: T[];
	currentPage: number;
	totalPages: number;
	totalCount: number;
	pageSize: number;
	totalActive: number;
	totalSubmitted: number;
	totalCountAll: number;
	hasPreviousPage: boolean;
	hasNextPage: boolean;
};

export type PaggingRequest = {
	pageIndex?: number;
	pageSize?: number;
	search?: string;
	ignorePagination?: boolean;
	partType?: number;
	materialType?: number;
	outputType?: number;
	scenarioType?: number;
	departmentId?: string;
	date?: string;
	longwallType?: number;
	maintainType?: number;
};

/**
 * Lấy headers với token
 */
const getHeaders = (): Record<string, string> => {
	const headers: Record<string, string> = {
		'Content-Type': 'application/json',
	};

	const token = authStorage.getToken();
	if (token) {
		headers.Authorization = `Bearer ${token}`;
	}

	return headers;
};

/**
 * Fetcher chính - check token trước, refresh nếu cần
 */
export const fetcher = async <Res, Req>(
	method: HttpMethod,
	path: string,
	query?: Record<string, string>,
	body?: Req,
	options: FetchOptions = {},
): Promise<BaseResponse<Res>> => {
	const { requiresAuth = true } = options;

	// Bước 1: Kiểm tra token
	// - Nếu không có token → logout
	// - Nếu access token hết hạn → refresh
	// - Nếu refresh token hết hạn → logout
	const tokens = requiresAuth ? await TokenRefreshService.ensureToken() : null;

	if (requiresAuth && !tokens) {
		// Token không hợp lệ, logout
		authStorage.clear();

		if (window.location.pathname !== '/auth/sign-in') {
			window.location.replace('/auth/sign-in');
		}

		throw new ErrorResponse({
			status: 401,
			message: 'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.',
		});
	}

	// Bước 2: Gửi request với token mới
	const search = new URLSearchParams(query).toString();
	const url = `${base}${path}${search ? '?' + search : ''}`;

	const response = await fetch(url, {
		method,
		body: JSON.stringify(body),
		headers: getHeaders(),
		cache: 'no-store',
	});

	const json = await response.json();

	if (!json.success) {
		throw new ErrorResponse(json);
	}

	return json as BaseResponse<Res>;
};

export const api = {
	get: async <Res>(
		path: string,
		query?: Record<string, string>,
		options?: FetchOptions,
	) => {
		return fetcher<Res, undefined>('GET', path, query, undefined, options);
	},
	post: async <Res, Req>(path: string, body: Req, options?: FetchOptions) => {
		return fetcher<Res, Req>('POST', path, undefined, body, options);
	},
	put: async <Res, Req>(path: string, body: Req, options?: FetchOptions) => {
		return fetcher<Res, Req>('PUT', path, undefined, body, options);
	},
	patch: async <Res, Req>(path: string, body: Req, options?: FetchOptions) => {
		return fetcher<Res, Req>('PATCH', path, undefined, body, options);
	},
	delete: async <Res, Req>(
		path: string,
		body?: Req,
		options?: FetchOptions,
	) => {
		return fetcher<Res, Req>('DELETE', path, undefined, body, options);
	},
	pagging: async <Res>(
		path: string,
		query: PaggingRequest = { ignorePagination: true },
		options?: FetchOptions,
	) => {
		return fetcher<PaggingResponse<Res>, undefined>(
			'GET',
			path,
			query as Record<string, string>,
			undefined,
			options,
		);
	},
	export: async (
		path: string,
		options?: {
			fileName?: string;
			forceFileName?: boolean;
			query?: Record<string, string>;
		},
	) => {
		const search = new URLSearchParams(options?.query).toString();
		const url = `${base}${path}${search ? '?' + search : ''}`;
		
		const headers = getHeaders();
		const response = await fetch(url, { headers });

		if (!response.ok) {
			const json = await response.json();
			throw new ErrorResponse(json);
		}

		// Get filename from content-disposition header
		const contentDisposition = response.headers.get('content-disposition');
		let filename = options?.fileName || 'download.xlsx'; // default filename

		if (contentDisposition && !options?.forceFileName) {
			const filenameMatch = contentDisposition.match(
				/filename\*?=['"]?(?:UTF-\d+'')?([^;\r\n"']*)['"]?/,
			);
			if (filenameMatch && filenameMatch[1]) {
				filename = decodeURIComponent(filenameMatch[1]);
			}
		}

		// Create blob and download
		const blob = await response.blob();
		const downloadUrl = window.URL.createObjectURL(blob);
		const a = document.createElement('a');
		a.href = downloadUrl;
		a.download = filename;
		document.body.appendChild(a);
		a.click();
		window.URL.revokeObjectURL(downloadUrl);
		document.body.removeChild(a);

		return filename;
	},
	import: async (
		path: string,
		file: File,
		fields?: Record<string, string | number | boolean>,
	) => {
		const url = `${base}${path}`;
		const formData = new FormData();
		formData.append('FormFile', file);
		if (fields) {
			Object.entries(fields).forEach(([key, value]) => {
				formData.append(key, String(value));
			});
		}

		const headers = getHeaders();
		delete headers['Content-Type']; // Let browser set Content-Type with boundary for FormData
		headers['Accept'] = 'application/octet-stream';

		const response = await fetch(url, {
			method: 'POST',
			body: formData,
			headers,
		});

		if (!response.ok) {
			const json = await response.json();
			throw new ErrorResponse(json);
		}

		const contentDisposition = response.headers.get('content-disposition');
		if (contentDisposition) {
			let filename = 'import_result.xlsx';
			const filenameMatch = contentDisposition.match(
				/filename\*?=['"]?(?:UTF-\d+'')?([^;\r\n"']*)['"]?/,
			);
			if (filenameMatch && filenameMatch[1]) {
				filename = decodeURIComponent(filenameMatch[1]);
			}

			const blob = await response.blob();
			const downloadUrl = window.URL.createObjectURL(blob);
			const a = document.createElement('a');
			a.href = downloadUrl;
			a.download = filename;
			document.body.appendChild(a);
			a.click();
			window.URL.revokeObjectURL(downloadUrl);
			document.body.removeChild(a);

			return filename;
		}

		if (response.headers.get('content-type')?.includes('application/json')) {
			const json = await response.json();
			return json;
		}
	},
	uploadFile: async <Res>(path: string, file: File) => {
		const url = `${base}${path}`;
		const formData = new FormData();
		formData.append('file', file);

		const headers = getHeaders();
		delete headers['Content-Type'];

		const response = await fetch(url, {
			method: 'POST',
			body: formData,
			headers,
		});

		const json = await response.json();

		if (!json.success) {
			throw new ErrorResponse(json);
		}

		return json as BaseResponse<Res>;
	},
};
