import type { ActionDialogProps } from '@/components/datatable';
import { DataTableEditConfirm } from '@/components/datatable/edit';
import { FormComboBox } from '@/components/form/form-combo-box';
import { FormMonthYear } from '@/components/form/form-month-year';
import { FormInput } from '@/components/form/form-input';
import { FormNumber } from '@/components/form/form-number';
import { FormProvider } from '@/components/form/form-provider';
import { FormRow } from '@/components/form/form-row';
import { FormSeparator } from '@/components/form/form-separator';
import { MultiSelect, type MultiSelectOption } from '@/components/multi-select';
import { usePopup } from '@/components/popup';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { API } from '@/constants/api-enpoint';
import { useDialog } from '@/data/dialog/dialog.hook';
import { useMeta } from '@/data/meta/meta-hook';
import { Asset } from '@/features/main/catalog/asset/types';
import type { ContractCode } from '@/features/main/catalog/contract-code/columns';
import type { Passport } from '@/features/main/catalog/parameter/passport/columns';
import type { Strength } from '@/features/main/catalog/parameter/strength/columns';
import { ProcessGroup } from '@/features/main/catalog/process/group/columns';
import { Slide } from '@/features/main/pricing/tunneling/slide/columns';
import {
	SLIDE_FORM_DEFAULT,
	slideFormSchema,
	SlideFormSchema,
} from '@/features/main/pricing/tunneling/slide/schema';
import { api } from '@/lib/api';
import { formatNumber } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import { XCircleIcon } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useForm, useFormContext, useWatch } from 'react-hook-form';

export type SlideDetail = {
	id: string;
	code: string;
	name: string;
	startMonth: string;
	endMonth: string;
	materialCost: Array<{
		assignmentCodeId: string;
		assignmentCode: string;
		assignmentCodeName: string;
		costs: Array<{
			materialId: string;
			materialCode: string;
			materialName: string;
			unitOfMeasureName: string;
			cost: number;
			amount: number;
		}>;
	}>;
};

function groupCostsByAssignmentCode(
	costs: SlideFormSchema['costs'],
	contracts: ContractCode[],
): Array<{
	assignmentCodeId: string;
	assignmentCode: string;
	assignmentCodeName: string;
	indices: number[];
	totalAmount: number;
}> {
	const grouped = new Map<
		string,
		{
			assignmentCodeId: string;
			assignmentCode: string;
			assignmentCodeName: string;
			indices: number[];
		}
	>();

	costs.forEach((cost, index) => {
		const assignmentCodeId = cost.assignmentCodeId;
		const contract = contracts.find((c) => c.id === assignmentCodeId);

		if (!grouped.has(assignmentCodeId)) {
			grouped.set(assignmentCodeId, {
				assignmentCodeId,
				assignmentCode: contract?.code || '',
				assignmentCodeName: contract?.name || '',
				indices: [],
			});
		}

		grouped.get(assignmentCodeId)?.indices.push(index);
	});

	const groups = Array.from(grouped.values()).sort((a, b) =>
		a.assignmentCode.localeCompare(b.assignmentCode),
	);

	// Calculate total amount for each group
	return groups.map((group) => {
		let totalAmount = 0;

		group.indices.forEach((index) => {
			const cost = costs[index];
			if (!cost) return;

			const amount = cost.amount || 0;
			if (!isNaN(amount)) {
				totalAmount += amount;
			}
		});

		return {
			...group,
			totalAmount,
		};
	});
}

export function SlideForm({
	data,
	row,
	isDuplicate = false,
}: ActionDialogProps<Slide> & { isDuplicate?: boolean }) {
	const popup = usePopup();
	const { setOpen } = useDialog();
	const { breadcrumb } = useMeta();
	const [groups, setGroups] = useState<ProcessGroup[]>([]);
	const [passports, setPassports] = useState<Passport[]>([]);
	const [strengths, setStrengths] = useState<Strength[]>([]);
	const [contracts, setContracts] = useState<ContractCode[]>([]);
	const [assets, setAssets] = useState<Asset[]>([]);
	const [selectedContracts, setSelectedContracts] = useState<
		MultiSelectOption[]
	>([]);

	const prevSelectedContractsRef = useRef<MultiSelectOption[]>([]);

	const form = useForm<SlideFormSchema>({
		resolver: zodResolver(slideFormSchema),
		mode: 'onSubmit',
		defaultValues: SLIDE_FORM_DEFAULT,
	});
	useEffect(() => {
		const promises = Promise.all([
			api.pagging<ProcessGroup>(API.CATALOG.PROCESS.GROUP.LIST),
			api.pagging<Passport>(API.CATALOG.PARAMETER.PASSPORT.LIST),
			api.pagging<Strength>(API.CATALOG.PARAMETER.STRENGTH.LIST),
			api.pagging<ContractCode>(API.CATALOG.CONTRACT_CODE.LIST),
			api.pagging<Asset>(API.CATALOG.ASSET.LIST, {
				...(row?.startMonth && { date: row.startMonth }),
				ignorePagination: true,
			}),
		]);

		promises.then(
			([processes, passportsRes, strengthsRes, contractsRes, assetsRes]) => {
				const processGroupsData = processes.result.data;
				const passportsData = passportsRes.result.data;
				const strengthsData = strengthsRes.result.data;
				const contractsData = contractsRes.result.data;
				const assetsData = assetsRes.result.data;

				setGroups(processGroupsData);
				setPassports(passportsData);
				setStrengths(strengthsData);
				setContracts(contractsData);
				setAssets(assetsData);

				if (row) {
					api.get<SlideDetail>(API.PRICING.SLIDE.DETAIL(row.id)).then((res) => {
						const { materialCost } = res.result;

						const contractCodeIds = materialCost.map(
							(cost) => cost.assignmentCodeId,
						);
						const selectedContractsFromAPI = contractsData
							.filter((contract) => contractCodeIds.includes(contract.id))
							.map<MultiSelectOption>((selected) => ({
								label: `${selected.code} - ${selected.name}`,
								value: selected.id,
							}));

						const costs: {
							assignmentCodeId: string;
							materialId: string;
							amount: number;
						}[] = [];

						materialCost.forEach((materialCostItem) => {
							materialCostItem.costs.forEach((cost) => {
								costs.push({
									assignmentCodeId: materialCostItem.assignmentCodeId,
									materialId: cost.materialId,
									amount: cost.amount,
								});
							});
						});

						prevSelectedContractsRef.current = selectedContractsFromAPI;

						form.reset({
							startMonth: row.startMonth.substring(0, 10),
							endMonth: row.endMonth.substring(0, 10),
							code: isDuplicate ? '' : row.code,
							processGroupId: row.processGroupId,
							passportId: row.passportId,
							hardnessId: row.hardnessId,
							costs: costs,
						});

						setSelectedContracts(selectedContractsFromAPI);
					});
				}
			},
		);
	}, [form, isDuplicate, row]);

	useEffect(() => {
		const prevContracts = prevSelectedContractsRef.current;
		const prevContractIds = new Set(prevContracts.map((c) => c.value));
		const currentContractIds = new Set(selectedContracts.map((c) => c.value));

		const addedContractIds = selectedContracts
			.filter((c) => !prevContractIds.has(c.value))
			.map((c) => c.value);

		const removedContractIds = prevContracts
			.filter((c) => !currentContractIds.has(c.value))
			.map((c) => c.value);

		const currentCosts = form.getValues('costs') || [];

		let updatedCosts = currentCosts.filter(
			(cost) => !removedContractIds.includes(cost.assignmentCodeId),
		);

		const newAssets = assets.filter((asset) =>
			addedContractIds.includes(asset.assignmentCodeId),
		);

		const newCosts = newAssets.map((asset) => ({
			assignmentCodeId: asset.assignmentCodeId,
			materialId: asset.id,
			amount: NaN,
		}));

		updatedCosts = [...updatedCosts, ...newCosts].sort((a, b) => {
			const aContract = contracts.find(
				(contract) => contract.id === a.assignmentCodeId,
			);
			const bContract = contracts.find(
				(contract) => contract.id === b.assignmentCodeId,
			);
			return (aContract?.code || '').localeCompare(bContract?.code || '');
		});

		form.setValue('costs', updatedCosts);
		prevSelectedContractsRef.current = selectedContracts;
	}, [selectedContracts, assets, form, contracts]);

	const handleRemoveAsset = useCallback(
		(index: number) => {
			const currentCosts = form.getValues('costs') || [];
			const removedAsset = currentCosts[index];

			if (!removedAsset) return;

			const updatedCosts = currentCosts.filter((_, i) => i !== index);
			form.setValue('costs', updatedCosts);

			const hasRemainingAssets = updatedCosts.some(
				(cost) => cost.assignmentCodeId === removedAsset.assignmentCodeId,
			);

			if (!hasRemainingAssets) {
				const updatedContracts = selectedContracts.filter(
					(c) => c.value !== removedAsset.assignmentCodeId,
				);
				prevSelectedContractsRef.current = updatedContracts;
				setSelectedContracts(updatedContracts);
			}
		},
		[form, selectedContracts],
	);

	const handleSubmit = async (values: SlideFormSchema) => {
		try {
			const processedValues = {
				...values,
			};
			if (row?.id && !isDuplicate) {
				await api.put(API.PRICING.SLIDE.UPDATE, {
					id: row.id,
					...processedValues,
				});
			} else {
				await api.post(API.PRICING.SLIDE.CREATE, processedValues);
			}

			setOpen(false);
			popup.success(
				`${breadcrumb} đã được ${row?.id && !isDuplicate ? 'Cập nhật' : 'Tạo mới'} thành công.`,
			);
			await data?.refresh();
			data?.table.toggleAllRowsSelected(false);
		} catch (error) {
			popup.error(error);
		}
	};

	return (
		<FormProvider context={form} onSubmit={handleSubmit}>
			<FormRow>
				<FormMonthYear
					control={form.control}
					name='startMonth'
					label='Thời gian bắt đầu'
					className='flex-1'
				/>
				<FormMonthYear
					control={form.control}
					name='endMonth'
					label='Thời gian kết thúc'
					className='flex-1'
				/>
			</FormRow>

			<FormSeparator />

			<FormInput
				control={form.control}
				name='code'
				label='Mã định mức máng trượt'
				placeholder='Nhập mã định mức máng trượt'
			/>

			<FormComboBox
				control={form.control}
				name='processGroupId'
				label='Nhóm công đoạn sản xuất'
				placeholder='Chọn nhóm công đoạn sản xuất'
				options={groups.map((process) => ({
					label: process.name,
					value: process.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='passportId'
				label='Hộ chiếu, Sđ, Sc'
				placeholder='Chọn hộ chiếu'
				options={passports.map((passport) => ({
					label: `H/c ${passport.name}; ${passport.sd}; ${passport.sc}`,
					value: passport.id,
				}))}
			/>

			<FormComboBox
				control={form.control}
				name='hardnessId'
				label='Độ kiên cố đá, than (f)'
				placeholder='Chọn Độ kiên cố đá, than (f)'
				options={strengths.map((strength) => ({
					label: strength.value,
					value: strength.id,
				}))}
			/>

			<MultiSelect
				label='Mã giao khoán'
				placeholder='Chọn mã giao khoán'
				values={selectedContracts}
				onValuesChange={setSelectedContracts}
				options={contracts.map((item) => ({
					value: item.id,
					label: `${item.code} - ${item.name}`,
				}))}
			/>

			{selectedContracts.length > 0 && (
				<GroupedMaterialCosts
					contracts={contracts}
					assets={assets}
					onRemove={handleRemoveAsset}
				/>
			)}

			<DataTableEditConfirm isEdit={!!row && !isDuplicate} />
		</FormProvider>
	);
}

function GroupedMaterialCosts({
	contracts,
	assets,
	onRemove,
}: {
	contracts: ContractCode[];
	assets: Asset[];
	onRemove: (index: number) => void;
}) {
	const { control } = useFormContext<SlideFormSchema>();
	const costs =
		useWatch({
			control,
			name: 'costs',
		}) || [];

	const groups = groupCostsByAssignmentCode(costs, contracts);

	if (groups.length === 0) return null;

	return (
		<div className='scrollbar-sm flex w-full flex-col gap-4 overflow-auto'>
			{groups.map((group) => (
				<div key={group.assignmentCodeId} className='flex flex-col gap-4'>
					<FormSeparator
						label={`${group.assignmentCodeName} - ${formatNumber(group.totalAmount)} (đ)`}
					/>
					{group.indices.map((index) => (
						<FormRow key={index}>
							<PricingMaterialCosts
								index={index}
								assets={assets}
								contracts={contracts}
								onRemove={() => onRemove(index)}
							/>
						</FormRow>
					))}
				</div>
			))}
		</div>
	);
}

function PricingMaterialCosts({
	index,
	contracts,
	assets,
	onRemove,
}: {
	index: number;
	contracts: ContractCode[];
	assets: Asset[];
	onRemove: () => void;
}) {
	const { control, getValues } = useFormContext();

	const assignmentCodeId = getValues(`costs.${index}.assignmentCodeId`);
	const materialId = getValues(`costs.${index}.materialId`);

	const contract = contracts.find((c) => c.id === assignmentCodeId);
	const asset = assets.find((a) => a.id === materialId);

	return (
		<>
			<div className='flex flex-1 flex-col gap-2'>
				<Label>Mã giao khoán</Label>
				<Input
					readOnly
					value={contract?.code}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<Label>Mã vật tư, tài sản</Label>
				<Input
					readOnly
					value={asset?.code}
					className='read-only:bg-transparent'
				/>
			</div>

			<div className='flex flex-1 flex-col gap-2'>
				<FormNumber
					control={control}
					name={`costs.${index}.amount`}
					label='Đơn giá máng trượt (đ/m)'
					placeholder='Nhập đơn giá máng trượt (đ/m)'
				/>
			</div>

			<Button
				type='button'
				variant='ghost'
				size='icon'
				className='text-error hover:text-error-muted disabled:text-muted-foreground mt-5.5 bg-transparent'
				onClick={onRemove}
			>
				<XCircleIcon className='size-6' />
			</Button>
		</>
	);
}
