import { FormControlProps } from '@/components/form/form-provider';
import {
	FormControl,
	FormField,
	FormItem,
	FormLabel,
	FormMessage,
} from '@/components/ui/form';
import { FieldValues } from 'react-hook-form';

// --- Các imports và type từ MultiSelect component ---
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
	Command,
	CommandEmpty,
	CommandGroup,
	CommandInput,
	CommandItem,
	CommandList,
} from '@/components/ui/command';
import { Label } from '@/components/ui/label';
import {
	Popover,
	PopoverContent,
	PopoverTrigger,
} from '@/components/ui/popover';
import { cn } from '@/lib/utils';
import { ChevronDownIcon, XCircleIcon } from 'lucide-react';
import { useState } from 'react';

// Giữ nguyên type MultiSelectOption
export type MultiSelectOption = {
	value: string;
	label: string;
};
// ------------------------------------------------------------------

// Type cho FormMultiSelect component (Giá trị lưu trong RHF vẫn là string[])
export type FormMultiSelectProps<T extends FieldValues> =
	FormControlProps<T> & {
		placeholder?: string;
		options?: MultiSelectOption[];
		disabled?: boolean;
	};

export function FormMultiSelect<T extends FieldValues>({
	control,
	name,
	label,
	placeholder,
	options = [],
	disabled = false,
}: FormMultiSelectProps<T>) {
	const [open, setOpen] = useState(false);

	return (
		<FormField
			control={control}
			name={name}
			render={({ field }) => {
				const selectedValues: string[] = Array.isArray(field.value)
					? field.value
					: [];

				const currentOptions: MultiSelectOption[] = options.filter((option) =>
					selectedValues.includes(option.value),
				);

				const handleRemove = (valueToRemove: string) => {
					if (disabled) return;
					const newSelectedValues = selectedValues.filter(
						(value) => value !== valueToRemove,
					);
					field.onChange(newSelectedValues);
				};

				const handleSelect = (option: MultiSelectOption) => {
					if (disabled) return;
					const isSelected = selectedValues.includes(option.value);

					let newSelectedValues: string[] = [];

					if (isSelected) {
						newSelectedValues = selectedValues.filter(
							(value) => value !== option.value,
						);
					} else {
						newSelectedValues = [...selectedValues, option.value];
					}

					field.onChange(newSelectedValues);
				};

				return (
					<FormItem className='flex flex-col gap-2'>
						<FormLabel>{label}</FormLabel>
						<FormControl>
							<Popover open={open} onOpenChange={setOpen}>
								<PopoverTrigger asChild>
									<Button
										variant={'ghost'}
										className={cn(
											'flex h-fit max-h-fit min-h-9 w-full flex-wrap rounded-sm border border-[#999999] bg-white px-3 hover:bg-white',
											open && 'border-primary border-2',
											selectedValues.length > 0
												? 'text-black'
												: 'text-muted-foreground',
											disabled && 'bg-transparent',
										)}
										disabled={disabled}
										ref={field.ref}
									>
										<div className='flex flex-1 flex-wrap justify-start gap-2 text-left'>
											{currentOptions.length > 0 ? (
												currentOptions.map((selected) => (
													<div
														key={selected.value}
														className='flex w-fit shrink-0 items-center gap-1 rounded-full bg-[#dfdfdf] px-2 py-0.5 text-xs text-black'
													>
														<span>{selected.label}</span>
														<div
															onClick={(event) => {
																event.stopPropagation();
																handleRemove(selected.value);
															}}
														>
															<XCircleIcon className='size-3.5' />
														</div>
													</div>
												))
											) : (
												<span>{placeholder}</span>
											)}
										</div>
										<ChevronDownIcon />
									</Button>
								</PopoverTrigger>

								<PopoverContent
									className='p-0'
									style={{ width: 'var(--radix-popover-trigger-width)' }}
									align='start'
								>
									<Command>
										<CommandInput placeholder='Tìm kiếm' />
										<CommandList
											className='max-h-58'
											onWheel={(e) => e.stopPropagation()}
										>
											<CommandEmpty>Không tìm thấy.</CommandEmpty>
											<CommandGroup>
												{options.map((option) => (
													<CommandItem
														value={option.label}
														key={option.value}
														className='inline-flex w-full items-center gap-2'
														onSelect={() => {
															handleSelect(option);
														}}
													>
														<Checkbox
															checked={selectedValues.includes(option.value)}
															className='[&_.lucide-check]:text-white'
														/>
														<Label className='font-normal'>
															{option.label}
														</Label>
													</CommandItem>
												))}
											</CommandGroup>
										</CommandList>
									</Command>
								</PopoverContent>
							</Popover>
							{/* Kết thúc tích hợp MultiSelect */}
						</FormControl>
						<FormMessage />
					</FormItem>
				);
			}}
		/>
	);
}
