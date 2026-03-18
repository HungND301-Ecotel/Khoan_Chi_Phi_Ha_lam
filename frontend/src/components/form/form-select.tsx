import { FormControlProps } from '@/components/form/form-provider';
import {
	FormControl,
	FormField,
	FormItem,
	FormLabel,
	FormMessage,
} from '@/components/ui/form';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { FieldValues } from 'react-hook-form';

export type FormSelectProps<T extends FieldValues> = FormControlProps<T> & {
	placeholder?: string;
	options?: { value: string; label: string }[];
};

export function FormSelect<T extends FieldValues>({
	control,
	name,
	label,
	placeholder,
	options = [],
}: FormSelectProps<T>) {
	return (
		<FormField
			control={control}
			name={name}
			render={({ field }) => {
				return (
					<FormItem>
						<FormLabel>{label}</FormLabel>
						<FormControl>
							<Select value={field.value} onValueChange={field.onChange}>
								<SelectTrigger className='w-full flex-1'>
									<SelectValue placeholder={placeholder} />
								</SelectTrigger>
								<SelectContent className='max-h-54'>
									{options.map((option) => (
										<SelectItem key={option.value} value={option.value}>
											{option.label}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</FormControl>
						<FormMessage />
					</FormItem>
				);
			}}
		/>
	);
}
