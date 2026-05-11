import { DataTableEditDialog } from '@/components/datatable/edit';
import {
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import { Button } from '@/components/ui/button';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemTitle,
} from '@/components/ui/item';
import { Spinner } from '@/components/ui/spinner';
import { API } from '@/constants/api-enpoint';
import { DialogProvider } from '@/data/dialog/dialog-provider';
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import {
	AcceptanceReportEditForm,
	MaterialImportDialog,
} from '@/features/main/cost/producttion/production/components/acceptance-report-editor';
import {
	AcceptanceReportDetail,
	AcceptanceReportItem,
	RawAcceptanceReportItem,
} from '@/features/main/cost/producttion/production/raw-acceptance-report/types';
import { api } from '@/lib/api';
import CreateIcon from '@mui/icons-material/Create';
import DownloadIcon from '@mui/icons-material/Download';
import UploadIcon from '@mui/icons-material/FileUpload';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';
import { RawAcceptanceReportDataTable } from './datatable';
import { exportMaterialTemplate } from '../material-import/export-template';

// Helper function to convert API response to RawAcceptanceReportItem
const convertAcceptanceReportItemToRawItem = (
	item: AcceptanceReportItem,
): RawAcceptanceReportItem => item;

export function RawAcceptanceReport({
	id,
	output,
	plan,
	callback,
	isOpen,
	reloadKey,
}: ProductCostExpandProps) {
	const [rawAcceptanceData, setRawAcceptanceData] = useState<
		RawAcceptanceReportItem[]
	>([]);
	const [error, setError] = useState<string | null>(null);
	const [isExporting, setIsExporting] = useState<boolean>(false);

	useEffect(() => {
		if (!isOpen || !output?.acceptanceReportId) {
			return;
		}

		const fetchAcceptanceReport = async () => {
			setError(null);
			try {
				const response = await api.get<AcceptanceReportDetail>(
					API.PRODUCTION.ACCEPTANCE_REPORT.RAW_DETAIL(
						output.acceptanceReportId!,
					),
				);

				if (response.result) {
					// Convert API items to RawAcceptanceReportItem format
					const convertedItems = response.result.items.map(
						convertAcceptanceReportItemToRawItem,
					);
					setRawAcceptanceData(convertedItems);
				}
			} catch (err) {
				console.error('Failed to fetch acceptance report:', err);
				setError(
					err instanceof Error
						? err.message
						: 'Failed to load acceptance report',
				);
				setRawAcceptanceData([]);
			}
		};

		fetchAcceptanceReport();
	}, [isOpen, output?.acceptanceReportId, reloadKey]);

	const handleImport = async () => {
		await callback?.();
	};

	const handleExport = async () => {
		if (!output?.acceptanceReportId) {
			exportMaterialTemplate();
			return;
		}

		setIsExporting(true);
		try {
			await api.export(
				API.PRODUCTION.ACCEPTANCE_REPORT.DOWNLOAD(output.acceptanceReportId),
			);
		} catch (err) {
			console.error('Failed to export acceptance report:', err);
		} finally {
			setIsExporting(false);
		}
	};

	return (
		<AccordionItem
			value={'raw-acceptance-report'}
			className='min-w-0 overflow-hidden border-none'
		>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Biên bản nghiệm thu</ItemTitle>
				</ItemContent>
				<ItemActions>
					{/* Import Button */}
					<DialogProvider>
						<DataTableEditDialog
							type='Tải lên'
							crumb='Biên bản nghiệm thu'
							trigger={
								<Button
									variant={'ghost'}
									size={'icon-sm'}
									className='size-5 rounded-full bg-transparent disabled:opacity-50'
								>
									<UploadIcon />
								</Button>
							}
						>
							<MaterialImportDialog
								onSave={handleImport}
								productionOutputId={output?.id}
							/>
						</DataTableEditDialog>
					</DialogProvider>

					{/* Export Button */}
					<Button
						variant={'ghost'}
						size={'icon-sm'}
						className='size-5 rounded-full bg-transparent disabled:opacity-50'
						disabled={isExporting}
						onClick={handleExport}
						title='Export'
					>
						{isExporting ? <Spinner /> : <DownloadIcon />}
					</Button>

					<AccordionTrigger
						disabled={!output?.acceptanceReportId}
						className='group p-0 disabled:opacity-50'
					>
						<div className='group-data-[state=open]:hidden'>
							<VisibilityIcon />
						</div>
						<div className='hidden group-data-[state=open]:block'>
							<VisibilityOffIcon />
						</div>
					</AccordionTrigger>

					{/* Update Button */}
					<DialogProvider>
						<DataTableEditDialog
							type='Chỉnh sửa'
							crumb='Biên bản nghiệm thu'
							trigger={
								<Button
									variant={'ghost'}
									size={'icon-sm'}
									className='size-5 rounded-full bg-transparent disabled:opacity-50'
									disabled={!output?.acceptanceReportId}
								>
									<CreateIcon />
								</Button>
							}
						>
							<AcceptanceReportEditForm
								id={id}
								plan={plan}
								output={output}
								callback={callback}
							/>
						</DataTableEditDialog>
					</DialogProvider>
				</ItemActions>
			</Item>

			{isOpen && (
				<AccordionContent className='max-h-96 overflow-hidden overflow-y-auto p-0 px-2 pt-2'>
					<div className='w-full min-w-0'>
						{error ? (
							<div className='border-border flex min-h-48 items-center justify-center rounded-t-md border bg-white shadow'>
								<div className='text-muted-foreground text-center'>
									<p className='text-lg font-medium'>Lỗi tải dữ liệu</p>
									<p className='text-sm'>{error}</p>
								</div>
							</div>
						) : (
							<RawAcceptanceReportDataTable items={rawAcceptanceData || []} />
						)}
					</div>
				</AccordionContent>
			)}
		</AccordionItem>
	);
}
