import { FormRow } from '@/components/form/form-row';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { PlusCircleIcon, XCircleIcon } from 'lucide-react';
import { JSX, useEffect } from 'react';
import {
	ArrayPath,
	Control,
	FieldArray,
	FieldValues,
	useFieldArray,
} from 'react-hook-form';

export type FormArrayProps<T extends FieldValues> = {
	control: Control<T>;
	name: string;
	label?: string;
	children: (index: number) => JSX.Element;
	hasCloseButton?: boolean;
	hasAddButton?: boolean;
	defaultValue?: any;
	canEmpty?: boolean;
};

export function FormArray<T extends FieldValues>({
	control,
	name,
	label,
	children,
	hasCloseButton = true,
	hasAddButton = true,
	defaultValue = {} as FieldArray<T, ArrayPath<T>>,
	canEmpty = false,
}: FormArrayProps<T>) {
	const { fields, append, remove } = useFieldArray({
		control,
		name: name as ArrayPath<T>,
	});

	// Auto append default value if fields are empty
	useEffect(() => {
		if (fields.length === 0) {
			append(defaultValue);
		}
	}, []);

	return (
		<div className='flex w-full flex-col gap-4'>
			{label && <Label>{label}</Label>}
			{fields.map((field, index) => {
				return (
					<FormRow key={field.id}>
						{children(index)}

						{hasCloseButton && (
							<Button
								type='button'
								variant='ghost'
								size='icon'
								className='text-error hover:text-error-muted disabled:text-muted-foreground mt-5.5 bg-transparent'
								onClick={() => remove(index)}
								disabled={canEmpty ? false : fields.length === 1}
							>
								<XCircleIcon className='size-6' />
							</Button>
						)}
					</FormRow>
				);
			})}

			{hasAddButton && (
				<Button
					type='button'
					variant='ghost'
					size='icon'
					className='h-fit w-fit bg-transparent'
					onClick={() => append(defaultValue)}
				>
					<PlusCircleIcon className='text-primary size-4' strokeWidth={2} />
					<span>Thêm {label}</span>
				</Button>
			)}
		</div>
	);
}
