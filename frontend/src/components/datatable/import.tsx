import { Button } from '@/components/ui/button';
import { DialogClose, DialogFooter } from '@/components/ui/dialog';
import { Spinner } from '@/components/ui/spinner';
import { UseDataTable } from '@/components/datatable/hook';
import { cn } from '@/lib/utils';
import CloseIcon from '@mui/icons-material/Close';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import InsertDriveFileIcon from '@mui/icons-material/InsertDriveFile';
import { useRef, useState } from 'react';

type DataTableImportProps<TData> = {
	data?: UseDataTable<TData>;
	onImport?: (file: File, data?: UseDataTable<TData>) => Promise<void> | void;
	isLoading?: boolean;
};

export function DataTableImport<TData>({
	data,
	onImport,
	isLoading,
}: DataTableImportProps<TData>) {
	const [file, setFile] = useState<File | null>(null);
	const [isDragging, setIsDragging] = useState(false);
	const [loading, setLoading] = useState(false);
	const inputRef = useRef<HTMLInputElement>(null);

	const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
		if (e.target.files && e.target.files.length > 0) {
			setFile(e.target.files[0]);
		}
	};

	const onDragOver = (e: React.DragEvent<HTMLDivElement>) => {
		e.preventDefault();
		setIsDragging(true);
	};

	const onDragLeave = (e: React.DragEvent<HTMLDivElement>) => {
		e.preventDefault();
		setIsDragging(false);
	};

	const onDrop = (e: React.DragEvent<HTMLDivElement>) => {
		e.preventDefault();
		setIsDragging(false);
		if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
			const droppedFile = e.dataTransfer.files[0];
			if (
				droppedFile.type ===
					'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' ||
				droppedFile.type === 'application/vnd.ms-excel' ||
				droppedFile.name.endsWith('.xlsx') ||
				droppedFile.name.endsWith('.xls')
			) {
				setFile(droppedFile);
			}
		}
	};

	const formatFileSize = (bytes: number) => {
		if (bytes === 0) return '0 Bytes';
		const k = 1024;
		const sizes = ['Bytes', 'KB', 'MB', 'GB'];
		const i = Math.floor(Math.log(bytes) / Math.log(k));
		return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
	};

	return (
		<>
			<div className='grid gap-4 py-4'>
				{!file ? (
					<div
						className={cn(
							'flex cursor-pointer flex-col items-center justify-center rounded-lg border-2 border-dashed p-10 transition-colors',
							isDragging
								? 'border-primary bg-primary/5'
								: 'border-muted-foreground/25 hover:bg-accent',
							isLoading && 'pointer-events-none opacity-50',
						)}
						onClick={() => !isLoading && inputRef.current?.click()}
						onDragOver={onDragOver}
						onDragLeave={onDragLeave}
						onDrop={onDrop}
					>
						<input
							ref={inputRef}
							type='file'
							accept='.xlsx, .xls'
							onChange={handleFileChange}
							className='hidden'
							disabled={isLoading}
						/>
						<CloudUploadIcon
							className='text-muted-foreground mb-4'
							style={{ fontSize: 48 }}
						/>
						<p className='mb-1 text-sm font-medium'>
							Kéo thả hoặc nhấn để chọn file
						</p>
						<p className='text-muted-foreground text-xs'>
							Hỗ trợ định dạng .xlsx, .xls (Tối đa 10MB)
						</p>
					</div>
				) : (
					<div className='flex items-center justify-between rounded-lg border p-4'>
						<div className='flex items-center gap-4'>
							<div className='bg-primary/10 flex h-10 w-10 items-center justify-center rounded'>
								<InsertDriveFileIcon className='text-primary' />
							</div>
							<div className='flex flex-col'>
								<span className='line-clamp-1 max-w-[200px] text-sm font-medium sm:max-w-xs'>
									{file.name}
								</span>
								<span className='text-muted-foreground text-xs'>
									{formatFileSize(file.size)}
								</span>
							</div>
						</div>
						<Button
							variant='ghost'
							size='icon'
							onClick={() => setFile(null)}
							className='text-muted-foreground hover:text-destructive h-8 w-8'
							disabled={loading}
						>
							<CloseIcon fontSize='small' />
						</Button>
					</div>
				)}
			</div>

			<DialogFooter>
				<DialogClose asChild>
					<Button variant='outline' disabled={loading}>
						Huỷ
					</Button>
				</DialogClose>
				<Button
					disabled={!file || loading}
					onClick={async () => {
						if (!file) return;
						setLoading(true);
						try {
							await onImport?.(file, data);
						} finally {
							setLoading(false);
						}
					}}
				>
					{loading ? (
						<>
							<Spinner className='mr-2 h-4 w-4' />
							Đang tải lên...
						</>
					) : (
						'Tải lên'
					)}
				</Button>
			</DialogFooter>
		</>
	);
}
