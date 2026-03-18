import { FormControlProps } from '@/components/form/form-provider';
import { Checkbox } from '@/components/ui/checkbox';
import {
	FormControl,
	FormField,
	FormItem,
	FormLabel,
	FormMessage,
} from '@/components/ui/form';
import { FieldValues } from 'react-hook-form';

export type FormCheckBoxProps<T extends FieldValues> = FormControlProps<T>;

export function FormCheckBox<T extends FieldValues>({
	control,
	name,
	label,
}: FormCheckBoxProps<T>) {
	return (
		<FormField
			control={control}
			name={name}
			render={({ field }) => {
				return (
					<FormItem className='w-full flex-1'>
						<FormControl>
							<div className='flex items-center gap-2'>
								<Checkbox
									id={field.name}
									checked={field.value}
									onCheckedChange={field.onChange}
									className='border-[#999999]'
								/>
								<FormLabel htmlFor={field.name}>{label}</FormLabel>
							</div>
						</FormControl>
						<FormMessage />
					</FormItem>
				);
			}}
		/>
	);
}
