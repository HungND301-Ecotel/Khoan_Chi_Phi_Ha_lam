import { FormControlProps } from '@/components/form/form-provider';
import {
	QUICK_CREATE_VALUE,
	QuickCreateHost,
	QuickCreateOption,
	useQuickCreate,
} from '@/components/form/quick-create';
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
import { PlusIcon } from 'lucide-react';
import { FieldValues } from 'react-hook-form';

export type FormSelectProps<T extends FieldValues> = FormControlProps<T> & {
	placeholder?: string;
	options?: { value: string; label: string }[];
	/** Thêm mục "Thêm mới ..." để tạo nhanh bản ghi của danh mục khác */
	quickCreate?: QuickCreateOption;
};

export function FormSelect<T extends FieldValues>({
	control,
	name,
	label,
	placeholder,
	options = [],
	quickCreate,
}: FormSelectProps<T>) {
	const quickCreateState = useQuickCreate();

	return (
		<FormField
			control={control}
			name={name}
			render={({ field }) => {
				return (
					<FormItem>
						{label && <FormLabel>{label}</FormLabel>}
						<FormControl>
							<Select
								key={field.value?.toString() + '_' + options.length}
								value={field.value?.toString() ?? ''}
								onValueChange={(value) => {
									if (value === QUICK_CREATE_VALUE) {
										quickCreateState.start(options);
										return;
									}
									field.onChange(value);
								}}
							>
								<SelectTrigger className='w-full flex-1'>
									<SelectValue placeholder={placeholder} />
								</SelectTrigger>
								<SelectContent className='max-h-54'>
									{quickCreate && (
										<SelectItem
											value={QUICK_CREATE_VALUE}
											className='text-primary'
										>
											<PlusIcon className='text-primary' />
											Thêm mới {quickCreate.label.toLowerCase()}
										</SelectItem>
									)}
									{options.map((option) => (
										<SelectItem key={option.value} value={option.value}>
											{option.label}
										</SelectItem>
									))}
								</SelectContent>
							</Select>
						</FormControl>
						{quickCreate && (
							<QuickCreateHost
								quickCreate={quickCreate}
								{...quickCreateState}
								options={options}
								onSelect={(value) => field.onChange(String(value))}
							/>
						)}
						<FormMessage />
					</FormItem>
				);
			}}
		/>
	);
}
