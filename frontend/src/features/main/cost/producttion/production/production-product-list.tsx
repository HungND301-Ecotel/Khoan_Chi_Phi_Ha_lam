import { DataTable } from '@/components/datatable';
import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from '@/components/ui/accordion';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemTitle,
} from '@/components/ui/item';
import { API } from '@/constants/api-enpoint';
import type { Product } from '@/features/main/catalog/product/columns';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { ColumnDef } from '@tanstack/react-table';
import { useEffect, useMemo, useState } from 'react';

type ProductionOutputProcessGroupProduct = {
	productId: string;
	productCode?: string;
	productName?: string;
	productionMeters: number;
};

type ProductionOutputProcessGroup = {
	processGroupId: string;
	processGroupCode?: string;
	processGroupName?: string;
	planProductionMeters?: number;
	standardProductionMeters?: number;
	productionMeters?: number;
	products?: ProductionOutputProcessGroupProduct[];
};

type ProductionOutputDetail = {
	processGroups?: ProductionOutputProcessGroup[];
};

type ProductionProductListProps = {
	productionOutputId: string;
	isOpen: boolean;
	reloadKey: number;
};

type ProductionGroupProductRow = {
	id: string;
	productCode: string;
	productName: string;
	productionMeters: number;
};

const PRODUCTION_GROUP_PRODUCT_COLUMNS: ColumnDef<ProductionGroupProductRow>[] =
	[
		{
			accessorKey: 'productCode',
			header: () => <span>Mã sản phẩm</span>,
		},
		{
			accessorKey: 'productName',
			header: () => <span>Tên sản phẩm</span>,
			cell: ({ row }) => (
				<span className='whitespace-normal'>{row.original.productName}</span>
			),
		},
		{
			accessorKey: 'productionMeters',
			header: () => <span>Sản lượng thực tế</span>,
			cell: ({ row }) => formatNumber(row.original.productionMeters ?? 0),
		},
	];

export function ProductionProductList({
	productionOutputId,
	isOpen,
	reloadKey,
}: ProductionProductListProps) {
	const [processGroups, setProcessGroups] = useState<
		ProductionOutputProcessGroup[]
	>([]);
	const [products, setProducts] = useState<Product[]>([]);
	const [error, setError] = useState<string | null>(null);
	const [openedGroups, setOpenedGroups] = useState<string[]>([]);

	useEffect(() => {
		if (!isOpen) return;

		const fetchData = async () => {
			setError(null);
			try {
				const [detailRes, productRes] = await Promise.all([
					api.get<ProductionOutputDetail>(
						API.PRODUCTION.PRODUCTION_OUTPUT.RAW_DETAIL(productionOutputId),
					),
					api.pagging<Product>(API.CATALOG.PRODUCT.LIST, {
						ignorePagination: true,
					}),
				]);

				setProcessGroups(detailRes.result.processGroups ?? []);
				setProducts(productRes.result.data ?? []);
			} catch (err) {
				console.error('Failed to load production output products:', err);
				setError(
					err instanceof Error
						? err.message
						: 'Không thể tải danh sách sản phẩm',
				);
				setProcessGroups([]);
			}
		};

		fetchData();
	}, [isOpen, productionOutputId, reloadKey]);

	const productMap = useMemo(
		() => new Map(products.map((product) => [product.id, product])),
		[products],
	);

	return (
		<AccordionItem
			value='production-product-list'
			className='min-w-0 overflow-hidden border-none'
		>
			<Item variant='outline' className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Danh sách sản phẩm</ItemTitle>
				</ItemContent>
				<ItemActions>
					<AccordionTrigger className='group p-0'>
						<div className='group-data-[state=open]:hidden'>
							<VisibilityIcon />
						</div>
						<div className='hidden group-data-[state=open]:block'>
							<VisibilityOffIcon />
						</div>
					</AccordionTrigger>
				</ItemActions>
			</Item>

			{isOpen && (
				<AccordionContent className='max-h-96 overflow-hidden overflow-y-auto p-0 px-2 pt-2'>
					{error ? (
						<div className='border-border flex min-h-48 items-center justify-center rounded-t-md border bg-white shadow'>
							<div className='text-muted-foreground text-center'>
								<p className='text-sm font-medium'>Lỗi tải dữ liệu</p>
								<p className='text-sm'>{error}</p>
							</div>
						</div>
					) : processGroups.length === 0 ? (
						<div className='border-border flex min-h-48 items-center justify-center rounded-t-md border bg-white shadow'>
							<div className='text-muted-foreground text-center'>
								<p className='text-sm font-medium'>Chưa có dữ liệu</p>
								<p className='text-sm'>
									Danh sách sản phẩm chưa được khai báo.
								</p>
							</div>
						</div>
					) : (
						<Accordion
							type='multiple'
							className='flex w-full min-w-0 flex-col gap-2'
							value={openedGroups}
							onValueChange={setOpenedGroups}
						>
							{processGroups.map((processGroup, index) => {
								const totalProductionMeters =
									processGroup.productionMeters ??
									(processGroup.products ?? []).reduce(
										(sum, product) => sum + (product.productionMeters ?? 0),
										0,
									);
								const groupKey =
									processGroup.processGroupId || `process-group-${index}`;
								const productRows: ProductionGroupProductRow[] = (
									processGroup.products ?? []
								).map((product) => {
									const catalogProduct = productMap.get(product.productId);

									return {
										id: `${groupKey}-${product.productId}`,
										productCode:
											product.productCode ?? catalogProduct?.code ?? '-',
										productName:
											product.productName ?? catalogProduct?.name ?? '-',
										productionMeters: product.productionMeters ?? 0,
									};
								});

								return (
									<AccordionItem
										key={groupKey}
										value={groupKey}
										className='min-w-0 overflow-hidden border-none'
									>
										<Item
											variant={'outline'}
											className='relative w-full flex-1 rounded-sm bg-gray-300 py-2'
										>
											<div className='flex w-full items-center gap-4'>
												<div className='flex flex-1 items-center'>
													<ItemTitle className='text-sm font-semibold'>
														{processGroup.processGroupCode
															? `${processGroup.processGroupCode} - ${processGroup.processGroupName || ''}`
															: processGroup.processGroupName ||
																'Nhóm công đoạn chưa xác định'}
													</ItemTitle>
												</div>
												<div className='me-2 w-12 text-sm font-semibold'>
													{formatNumber(
														processGroup.standardProductionMeters ?? 0,
													)}
												</div>
												<div className='w-24 text-sm font-semibold'>
													{formatNumber(totalProductionMeters)}
												</div>
												<ItemActions>
													<AccordionTrigger className='group p-0'>
														<div className='group-data-[state=open]:hidden'>
															<VisibilityIcon />
														</div>
														<div className='hidden group-data-[state=open]:block'>
															<VisibilityOffIcon />
														</div>
													</AccordionTrigger>
												</ItemActions>
											</div>
										</Item>

										<AccordionContent className='p-0 pt-2'>
											<div className='w-full min-w-0 overflow-x-auto'>
												<DataTable
													columns={PRODUCTION_GROUP_PRODUCT_COLUMNS}
													items={productRows}
													hasActions={false}
													hasPagination={false}
													hasSort={false}
													hasIndex={false}
													compact={true}
												/>
											</div>
										</AccordionContent>
									</AccordionItem>
								);
							})}
						</Accordion>
					)}
				</AccordionContent>
			)}
		</AccordionItem>
	);
}
