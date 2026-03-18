import { FormControlProps } from '@/components/form/form-provider';
import {
	FormControl,
	FormField,
	FormItem,
	FormLabel,
	FormMessage,
} from '@/components/ui/form';
import { Textarea } from '@/components/ui/textarea';
import { ComponentProps } from 'react';
import { ControllerRenderProps, FieldValues, Path } from 'react-hook-form';

export type FormTextProps<T extends FieldValues> = FormControlProps<T> &
	Omit<
		ComponentProps<'textarea'>,
		keyof FormControlProps<T> | keyof ControllerRenderProps<T, Path<T>> | 'id'
	>;

export function FormText<T extends FieldValues>({
	control,
	name,
	label,
	autoComplete = 'off',
	...props
}: FormTextProps<T>) {
	return (
		<FormField
			control={control}
			name={name}
			render={({ field }) => {
				return (
					<FormItem className='w-full flex-1'>
						<FormLabel>{label}</FormLabel>
						<FormControl>
							<Textarea autoComplete={autoComplete} {...props} {...field} />
						</FormControl>
						<FormMessage />
					</FormItem>
				);
			}}
		/>
	);
}
