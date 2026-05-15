import { FormComboBox } from '@/components/form/form-combo-box';
import { FormProvider } from '@/components/form/form-provider';
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useMemo } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import {
	UNRESOLVED_CREATE_DEFAULT,
	unresolvedCreateSchema,
	UnresolvedCreateSchema,
} from './unresolved-create-schema';

type UnresolvedCreateDialogProps = {
	open: boolean;
	onOpenChange: (open: boolean) => void;
	defaultCode: string;
	onSubmit: (values: UnresolvedCreateSchema) => Promise<void>;
};

const ENTITY_GROUP_OPTIONS = [
	{ value: 'material', label: 'Vật tư' },
	{ value: 'part', label: 'Vật tư SCTX' },
] as const;

const MATERIAL_TYPE_OPTIONS = [
	{ value: 1, label: 'Vật tư, tài sản' },
	{ value: 2, label: 'Vật tư, tài sản khác' },
] as const;

const PART_TYPE_OPTIONS = [
	{ value: 1, label: 'Vật tư theo nhóm vật tư, tài sản' },
	{ value: 2, label: 'Vật tư khác' },
] as const;

export function UnresolvedCreateDialog({
	open,
	onOpenChange,
	defaultCode,
	onSubmit,
}: UnresolvedCreateDialogProps) {
	const form = useForm<UnresolvedCreateSchema>({
		resolver: zodResolver(unresolvedCreateSchema),
		defaultValues: UNRESOLVED_CREATE_DEFAULT,
		mode: 'onSubmit',
	});

	const entityGroup = useWatch({
		control: form.control,
		name: 'entityGroup',
	});
	const specificType = useWatch({
		control: form.control,
		name: 'specificType',
	});

	useEffect(() => {
		if (!open) {
			return;
		}

		form.reset({
			...UNRESOLVED_CREATE_DEFAULT,
			specificType: 1,
		});
	}, [defaultCode, form, open]);

	useEffect(() => {
		if (
			entityGroup === 'material' &&
			!MATERIAL_TYPE_OPTIONS.some((option) => option.value === specificType)
		) {
			form.setValue('specificType', 1);
			return;
		}

		if (
			entityGroup === 'part' &&
			!PART_TYPE_OPTIONS.some((option) => option.value === specificType)
		) {
			form.setValue('specificType', 1);
		}
	}, [entityGroup, form, specificType]);

	const specificTypeOptions = useMemo(
		() =>
			entityGroup === 'material'
				? [...MATERIAL_TYPE_OPTIONS]
				: [...PART_TYPE_OPTIONS],
		[entityGroup],
	);

	const handleSubmit = async (values: UnresolvedCreateSchema) => {
		await onSubmit(values);
	};

	return (
		<Dialog open={open} onOpenChange={onOpenChange}>
			<DialogContent className='max-w-xl' showCloseButton={false}>
				<DialogHeader>
					<DialogTitle>Chọn loại đối tượng cần tạo mới</DialogTitle>
					<DialogDescription>
						Dòng mã {defaultCode || 'chưa xác định'} chưa tồn tại trong danh
						mục. Chọn nhóm và loại để mở đúng popup tạo mới đang được dùng ở màn
						hình danh mục.
					</DialogDescription>
				</DialogHeader>
				<FormProvider context={form} onSubmit={handleSubmit}>
					<div className='grid gap-4 md:grid-cols-2'>
						<FormComboBox
							control={form.control}
							name='entityGroup'
							label='Nhóm đối tượng'
							placeholder='Chọn nhóm'
							options={ENTITY_GROUP_OPTIONS.map((option) => ({
								value: option.value,
								label: option.label,
							}))}
						/>
						<FormComboBox
							control={form.control}
							name='specificType'
							label='Loại'
							placeholder='Chọn loại'
							options={specificTypeOptions.map((option) => ({
								value: option.value,
								label: option.label,
							}))}
						/>
					</div>
					<DialogFooter className='mt-6'>
						<Button
							type='button'
							variant='outline'
							onClick={() => onOpenChange(false)}
						>
							Huỷ
						</Button>
						<Button type='submit' disabled={form.formState.isSubmitting}>
							{form.formState.isSubmitting ? <Spinner /> : 'Tạo mới'}
						</Button>
					</DialogFooter>
				</FormProvider>
			</DialogContent>
		</Dialog>
	);
}
