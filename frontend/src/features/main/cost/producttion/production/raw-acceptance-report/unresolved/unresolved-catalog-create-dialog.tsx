import { ActionDialogProps } from '@/components/datatable';
import { DataTableEditDialog } from '@/components/datatable/edit';
import { DialogContext } from '@/data/dialog/dialog-context';
import { AssetInternalForm } from '@/features/main/catalog/asset/internal/form';
import { Asset } from '@/features/main/catalog/asset/types';
import { ReactNode, useMemo } from 'react';

export type UnresolvedCatalogCreateSelection = {
	entityGroup: 'material';
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
	return 'vật tư';
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

		const successLabel =
			selection.specificType === 2 ? 'Vật tư, tài sản khác' : 'Vật tư, tài sản';
		return (
			<AssetInternalForm
				data={embeddedData as ActionDialogProps<Asset>['data']}
				defaultCode={defaultCode}
				successLabel={successLabel}
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
