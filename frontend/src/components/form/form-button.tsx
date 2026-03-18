import { FormControlProps } from '@/components/form/form-provider';
import { Button } from '@/components/ui/button';
import {
	FormControl,
	FormField,
	FormItem,
	FormLabel,
	FormMessage,
} from '@/components/ui/form';
import { ComponentProps } from 'react';
import { ControllerRenderProps, FieldValues, Path } from 'react-hook-form';

export type FormButtonProps<T extends FieldValues> = FormControlProps<T> &
	Omit<
		ComponentProps<'button'>,
		keyof FormControlProps<T> | keyof ControllerRenderProps<T, Path<T>> | 'id'
	> & {
		title?: string;
	};

export function FormButton<T extends FieldValues>({
	control,
	name,
	label,
	title,
}: FormButtonProps<T>) {
	return (
		<FormField
			control={control}
			name={name}
			render={({ field }) => {
				return (
					<FormItem className='w-full flex-1'>
						<FormLabel>{label}</FormLabel>
						<FormControl>
							<Button value={field.value}>{title}</Button>
						</FormControl>
						<FormMessage />
					</FormItem>
				);
			}}
		/>
	);
}
