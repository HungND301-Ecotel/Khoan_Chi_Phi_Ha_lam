import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditDialog } from '@/components/datatable/edit';
import { DialogContext } from '@/data/dialog/dialog-context';
import { AssetExternalForm } from '@/features/main/catalog/asset/external/form';
import { AssetInternalForm } from '@/features/main/catalog/asset/internal/form';
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
				return 'vật tư, tài sản';
			case 2:
				return 'vật tư, tài sản khác';
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
							successLabel='Vật tư, tài sản'
							onCreated={onCreated}
						/>
					);
				case 2:
					return (
						<AssetExternalForm
							data={embeddedData as ActionDialogProps<Asset>['data']}
							defaultCode={defaultCode}
							successLabel='Vật tư, tài sản khác'
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
					successLabel='Vật tư theo nhóm vật tư, tài sản'
					onCreated={onCreated}
				/>
			);
		}

		return (
			<OtherPartForm
				data={embeddedData as ActionDialogProps<OtherPart>['data']}
				defaultCode={defaultCode}
				successLabel='Vật tư khác'
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
