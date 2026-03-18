import { DataTable } from '@/components/datatable';
import {
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
import { ProductCostExpandProps } from '@/features/main/cost/plan/types';
import {
	AdditionalCostResponse,
	MATERIAL_COLUMNS,
	MaterialCost,
	OTHER_MATERIALS_COLUMNS,
	OtherMaterial,
	SCTXCost,
	SCTX_COLUMNS,
} from '@/features/main/cost/producttion/production/additional-cost/columns';
import { api } from '@/lib/api';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import { useEffect, useState } from 'react';

export function AdditionalCost({ output, isOpen }: ProductCostExpandProps) {
	const [materials, setMaterials] = useState<MaterialCost[]>([]);
	const [sctx, setSCTX] = useState<SCTXCost[]>([]);
	const [otherMaterials, setOtherMaterials] = useState<OtherMaterial[]>([]);

	useEffect(() => {
		if (!output?.acceptanceReportId) {
			return;
		}

		const fetchAdditionalCost = async () => {
			try {
				const response = await api.get<AdditionalCostResponse>(
					API.PRODUCTION.ACCEPTANCE_REPORT.ADDITIONAL_COST_LIST(
						output.acceptanceReportId!,
					),
				);

				if (response.result) {
					const data = response.result.additionalCosts;

					// Transform material data
					const materialItems: MaterialCost[] = (data.material || []).map(
						(item, idx) => ({
							id: `${item.code}-${idx}`,
							materialCode: item.code,
							materialName: item.name,
							unitOfMeasure: item.unitOfMeasureName,
							quantity: item.additionalCostQuantity,
						}),
					);

					// Transform maintain data (SCTX)
					const sctxItems: SCTXCost[] = (data.maintain || []).map(
						(item, idx) => ({
							id: `${item.code}-${idx}`,
							partCode: item.code,
							partName: item.name,
							unitOfMeasure: item.unitOfMeasureName,
							quantity: item.additionalCostQuantity,
						}),
					);

					// Transform other material data
					const otherItems: OtherMaterial[] = (data.otherMaterial || []).map(
						(item, idx) => ({
							id: `${item.code}-${idx}`,
							materialCode: item.code,
							materialName: item.name,
							unitOfMeasure: item.unitOfMeasureName,
							quantity: item.additionalCostQuantity,
						}),
					);

					setMaterials(materialItems);
					setSCTX(sctxItems);
					setOtherMaterials(otherItems);
				}
			} catch (err) {
				console.error('Failed to fetch additional cost:', err);
			}
		};

		fetchAdditionalCost();
	}, [output?.acceptanceReportId]);

	return (
		<AccordionItem
			value={'additional-cost'}
			className='min-w-0 overflow-hidden border-none'
		>
			<Item variant={'outline'} className='w-full flex-1 rounded-sm py-3'>
				<ItemContent>
					<ItemTitle>Bổ sung chi phí</ItemTitle>
				</ItemContent>
				<ItemActions>
					<div className='size-5'></div>
					<div className='size-5'></div>
					<div className='size-5'></div>
					<AccordionTrigger
						disabled={false}
						className='group p-0 disabled:opacity-50'
					>
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
					<div className='w-full min-w-0 overflow-x-auto'>
						<div className='flex flex-col gap-6'>
							{/* Material Section */}
							<div className='flex flex-col gap-2'>
								{materials && materials.length > 0 && (
									<>
										<h4 className='text-sm font-semibold'>Vật liệu</h4>
										<DataTable
											columns={MATERIAL_COLUMNS}
											items={materials}
											compact={true}
											hasActions={false}
											hasPagination={false}
											hasSort={false}
											hasIndex={false}
										/>
									</>
								)}
							</div>

							{/* SCTX Section */}
							<div className='flex flex-col gap-2'>
								{sctx && sctx.length > 0 && (
									<>
										<h4 className='text-sm font-semibold'>SCTX</h4>
										<DataTable
											columns={SCTX_COLUMNS}
											items={sctx}
											compact={true}
											hasActions={false}
											hasPagination={false}
											hasSort={false}
											hasIndex={false}
										/>
									</>
								)}
							</div>

							{/* Other Materials Section */}
							<div className='flex flex-col gap-2'>
								{otherMaterials && otherMaterials.length > 0 && (
									<>
										<h4 className='text-sm font-semibold'>Vật tư khác</h4>
										<DataTable
											columns={OTHER_MATERIALS_COLUMNS}
											items={otherMaterials}
											compact={true}
											hasActions={false}
											hasPagination={false}
											hasSort={false}
											hasIndex={false}
										/>
									</>
								)}
							</div>
						</div>
					</div>
				</AccordionContent>
			)}
		</AccordionItem>
	);
}
