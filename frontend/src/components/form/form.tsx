import { cn } from '@/lib/utils';
import { ComponentProps } from 'react';
import {
	Control,
	FieldValues,
	FormProvider,
	Path,
	UseFormReturn,
} from 'react-hook-form';

export type FormProps<T extends FieldValues> = {
	context: UseFormReturn<T>;
	onSubmit?: (values: T) => Promise<void> | void;
} & Omit<ComponentProps<'form'>, 'onSubmit'>;

export function Form<T extends FieldValues>({
	context,
	onSubmit,
	className,
	...props
}: FormProps<T>) {
	return (
		<FormProvider {...context}>
			<form
				onSubmit={onSubmit && context.handleSubmit(onSubmit)}
				className={cn('flex flex-col gap-4', className)}
				{...props}
			>
				{props.children}
			</form>
		</FormProvider>
	);
}

export type FormControlProps<T extends FieldValues> = {
	control: Control<T>;
	name: Path<T>;
	label?: string;
	disabled?: boolean;
};
