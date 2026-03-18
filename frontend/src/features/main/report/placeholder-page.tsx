'use client';

interface ReportCategoryPlaceholderPageProps {
	categoryLabel: string;
}

export function ReportCategoryPlaceholderPage({
	categoryLabel,
}: ReportCategoryPlaceholderPageProps) {
	return (
		<div className='border-border flex h-96 items-center justify-center rounded-t-md border bg-white shadow'>
			<div className='text-muted-foreground text-center'>
				<p className='text-lg font-medium'>{categoryLabel}</p>
				<p className='text-sm'>Trang đang được phát triển</p>
			</div>
		</div>
	);
}
