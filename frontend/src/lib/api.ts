export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
const base = import.meta.env.VITE_API_BASE_URL;

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
	type?: number;
	partType?: number;
	materialType?: number;
	outputType?: number;
	scenarioType?: number;
	departmentId?: string;
	date?: string;
	longwallType?: number;
	maintainType?: number;
};

export const fetcher = async <Res, Req>(
	method: HttpMethod,
	path: string,
	query?: Record<string, string>,
	body?: Req,
) => {
	const search = new URLSearchParams(query).toString();
	const url = `${base}${path}${search ? '?' + search : ''}`;

	const response = await fetch(url, {
		method,
		body: JSON.stringify(body),
		headers: {
			'Content-Type': 'application/json',
		},
	});

	const json = await response.json();

	if (!json.success) {
		throw new ErrorResponse(json);
	}

	return json as BaseResponse<Res>;
};

export const api = {
	get: async <Res>(path: string, query?: Record<string, string>) => {
		return fetcher<Res, undefined>('GET', path, query);
	},
	post: async <Res, Req>(path: string, body: Req) => {
		return fetcher<Res, Req>('POST', path, undefined, body);
	},
	put: async <Res, Req>(path: string, body: Req) => {
		return fetcher<Res, Req>('PUT', path, undefined, body);
	},
	patch: async <Res, Req>(path: string, body: Req) => {
		return fetcher<Res, Req>('PATCH', path, undefined, body);
	},
	delete: async <Res, Req>(path: string, body?: Req) => {
		return fetcher<Res, Req>('DELETE', path, undefined, body);
	},
	pagging: async <Res>(
		path: string,
		query: PaggingRequest = { ignorePagination: true },
	) => {
		return fetcher<PaggingResponse<Res>, undefined>(
			'GET',
			path,
			query as Record<string, string>,
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
		const response = await fetch(url);

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

		const response = await fetch(url, {
			method: 'POST',
			body: formData,
			headers: {
				Accept: 'application/octet-stream',
			},
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

		const response = await fetch(url, {
			method: 'POST',
			body: formData,
		});

		const json = await response.json();

		if (!json.success) {
			throw new ErrorResponse(json);
		}

		return json as BaseResponse<Res>;
	},
};
