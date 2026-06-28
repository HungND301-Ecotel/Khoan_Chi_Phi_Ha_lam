import { Form } from '@/components/ui/form';
import { cn } from '@/lib/utils';
import { ComponentProps } from 'react';
import {
	Control,
	FieldErrors,
	FieldValues,
	Path,
	UseFormReturn,
} from 'react-hook-form';

export type HandleSubmit<T extends FieldValues> = (
	values: T,
) => void | Promise<void>;

export type HandleInvalidSubmit<T extends FieldValues> = (
	errors: FieldErrors<T>,
) => void;

export type FormProps<T extends FieldValues> = {
	context: UseFormReturn<T>;
	onSubmit: HandleSubmit<T>;
	onInvalid?: HandleInvalidSubmit<T>;
} & Omit<ComponentProps<'form'>, 'onSubmit'>;

export type FormControlProps<T extends FieldValues> = {
	control: Control<T>;
	name: Path<T>;
	label?: string;
};

export function FormProvider<T extends FieldValues>({
	onSubmit,
	onInvalid,
	context,
	className,
	...props
}: FormProps<T>) {
	return (
		<Form {...context}>
			<form
				onSubmit={context.handleSubmit(onSubmit, onInvalid)}
				className={cn('flex flex-col gap-4', className)}
				{...props}
			/>
		</Form>
	);
}
