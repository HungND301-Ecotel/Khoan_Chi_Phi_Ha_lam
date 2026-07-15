import { useState, useEffect, useCallback } from 'react';
import { authStorage } from '@/lib/auth-storage';

export type UploadOptions = {
	endpoint?: string;
	fileKey?: string;
	queryParams?: Record<string, string>;
};

export function useFileUpload() {
	const [file, setFile] = useState<File | null>(null);
	const [previewUrl, setPreviewUrl] = useState<string>('');
	const [uploading, setUploading] = useState(false);

	useEffect(() => {
		return () => {
			if (previewUrl && previewUrl.startsWith('blob:')) {
				URL.revokeObjectURL(previewUrl);
			}
		};
	}, [previewUrl]);

	// Chọn file cục bộ và tạo URL xem trước tạm thời (Local URL)
	const selectFile = useCallback((selectedFile: File) => {
		setFile(selectedFile);
		const objectUrl = URL.createObjectURL(selectedFile);
		setPreviewUrl(objectUrl);
	}, []);

	const clear = useCallback(() => {
		setFile(null);
		setPreviewUrl('');
	}, []);

	const upload = async (options?: UploadOptions): Promise<string> => {
		if (!file) {
			throw new Error('Chưa có file nào được chọn');
		}

		const {
			endpoint = '/v1/User/Employee/upload-image',
			fileKey = 'Files',
			queryParams = {},
		} = options || {};

		setUploading(true);
		try {
			const formData = new FormData();
			formData.append(fileKey, file);

			const searchParams = new URLSearchParams(queryParams).toString();
			const url = `${import.meta.env.VITE_API_BASE_URL}${endpoint}${searchParams ? '?' + searchParams : ''}`;

			const response = await fetch(url, {
				method: 'POST',
				body: formData,
				headers: {
					Authorization: `Bearer ${authStorage.getToken()}`,
				},
			});

			if (!response.ok) {
				const json = await response.json();
				throw new Error(json.message || 'Lỗi tải tệp tin lên máy chủ');
			}

			const data = await response.json();
			if (data.success && data.result && data.result.length > 0) {
				return data.result[0];
			}
			throw new Error('Không nhận được đường dẫn từ máy chủ');
		} finally {
			setUploading(false);
		}
	};

	return {
		file,
		previewUrl,
		uploading,
		selectFile,
		clear,
		upload,
		setPreviewUrl, // Cho phép trang cha gán link S3 ban đầu khi tải profile
	};
}
