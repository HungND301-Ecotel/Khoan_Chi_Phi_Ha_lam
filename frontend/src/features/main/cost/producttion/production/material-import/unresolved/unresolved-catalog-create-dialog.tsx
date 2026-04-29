import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditDialog } from '@/components/datatable/edit';
import { DialogContext } from '@/data/dialog/dialog-context';
import { AssetExternalForm } from '@/features/main/catalog/asset/external/form';
import { AssetInternalForm } from '@/features/main/catalog/asset/internal/form';
import { AssetQuotaMaterialsForm } from '@/features/main/catalog/asset/quota-materials/form';
import { AssetResourceForm } from '@/features/main/catalog/asset/resource/form';
import { AssetSafetyAndWelfareForm } from '@/features/main/catalog/asset/safety-and-welfare/form';
import { Asset } from '@/features/main/catalog/asset/types';
import { Part } from '@/features/main/catalog/part/main/columns';
import { PartForm } from '@/features/main/catalog/part/main/actions';
import { OtherPart } from '@/features/main/catalog/part/other/columns';
import { OtherPartForm } from '@/features/main/catalog/part/other/actions';
import { ReactNode, useMemo } from 'react';

export type UnresolvedCatalogCreateSelection = {
	entityGroup: 'material' | 'part';
	specificType: number;
};

type UnresolvedCatalogCreateDialogProps = {
	open: boolean;
	onOpenChange: (open: boolean) => void;
	selection: UnresolvedCatalogCreateSelection | null;
	defaultCode: string;
	onCreated: (values: { code: string }) => Promise<void>;
};

function createEmbeddedData<TData>(): ActionDialogProps<TData>['data'] {
	return {
		refresh: async () => {},
		table: {
			toggleAllRowsSelected: () => {},
		},
	} as ActionDialogProps<TData>['data'];
}

function getSelectionLabel(
	selection: UnresolvedCatalogCreateSelection | null,
): string {
	if (!selection) return 'đối tượng';

	if (selection.entityGroup === 'material') {
		switch (selection.specificType) {
			case 1:
				return 'vật tư, tài sản trong khoán';
			case 2:
				return 'vật tư, tài sản ngoài khoán';
			case 3:
				return 'vật tư theo chế độ người lao động';
			case 4:
				return 'tài sản';
			case 5:
				return 'vật tư theo hạn mức';
			default:
				return 'vật tư';
		}
	}

	switch (selection.specificType) {
		case 1:
			return 'phụ tùng theo thiết bị';
		case 2:
			return 'phụ tùng khác';
		default:
			return 'phụ tùng';
	}
}

export function UnresolvedCatalogCreateDialog({
	open,
	onOpenChange,
	selection,
	defaultCode,
	onCreated,
}: UnresolvedCatalogCreateDialogProps) {
	const embeddedData = useMemo(() => createEmbeddedData<unknown>(), []);
	const label = getSelectionLabel(selection);

	const content = useMemo<ReactNode>(() => {
		if (!selection) {
			return null;
		}

		if (selection.entityGroup === 'material') {
			switch (selection.specificType) {
				case 1:
					return (
						<AssetInternalForm
							data={embeddedData as ActionDialogProps<Asset>['data']}
							defaultCode={defaultCode}
							successLabel='Vật tư, tài sản trong khoán'
							onCreated={onCreated}
						/>
					);
				case 2:
					return (
						<AssetExternalForm
							data={embeddedData as ActionDialogProps<Asset>['data']}
							defaultCode={defaultCode}
							successLabel='Vật tư, tài sản ngoài khoán'
							onCreated={onCreated}
						/>
					);
				case 3:
					return (
						<AssetSafetyAndWelfareForm
							data={embeddedData as ActionDialogProps<Asset>['data']}
							defaultCode={defaultCode}
							successLabel='Vật tư theo chế độ người lao động'
							onCreated={onCreated}
						/>
					);
				case 4:
					return (
						<AssetResourceForm
							data={embeddedData as ActionDialogProps<Asset>['data']}
							defaultCode={defaultCode}
							successLabel='Tài sản'
							onCreated={onCreated}
						/>
					);
				case 5:
					return (
						<AssetQuotaMaterialsForm
							data={embeddedData as ActionDialogProps<Asset>['data']}
							defaultCode={defaultCode}
							successLabel='Vật tư theo hạn mức'
							onCreated={onCreated}
						/>
					);
			}
		}

		if (selection.specificType === 1) {
			return (
				<PartForm
					data={embeddedData as ActionDialogProps<Part>['data']}
					defaultCode={defaultCode}
					successLabel='Phụ tùng theo thiết bị'
					onCreated={onCreated}
				/>
			);
		}

		return (
			<OtherPartForm
				data={embeddedData as ActionDialogProps<OtherPart>['data']}
				defaultCode={defaultCode}
				successLabel='Phụ tùng khác'
				onCreated={onCreated}
			/>
		);
	}, [defaultCode, embeddedData, onCreated, selection]);

	return (
		<DialogContext.Provider value={{ open, setOpen: onOpenChange }}>
			<DataTableEditDialog
				type='Tạo mới'
				crumb={label}
				trigger={<span className='hidden' aria-hidden='true' />}
			>
				{content}
			</DataTableEditDialog>
		</DialogContext.Provider>
	);
}
