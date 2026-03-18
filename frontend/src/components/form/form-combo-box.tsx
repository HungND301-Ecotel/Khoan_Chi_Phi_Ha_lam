import { FormControlProps } from '@/components/form/form-provider';
import {
	Command,
	CommandEmpty,
	CommandGroup,
	CommandInput,
	CommandItem,
	CommandList,
} from '@/components/ui/command';
import {
	FormControl,
	FormField,
	FormItem,
	FormLabel,
	FormMessage,
} from '@/components/ui/form';
import {
	InputGroup,
	InputGroupAddon,
	InputGroupInput,
} from '@/components/ui/input-group';
import {
	Popover,
	PopoverContent,
	PopoverTrigger,
} from '@/components/ui/popover';
import { cn } from '@/lib/utils';
import { Check, ChevronDownIcon, XIcon } from 'lucide-react';
import * as React from 'react';
import { FieldValues } from 'react-hook-form';

export type FormComboBoxProps<T extends FieldValues> = FormControlProps<T> & {
	placeholder?: string;
	options?: { value: string | number; label: string }[];
	readonly?: boolean;
	disabled?: boolean;
};

export function FormComboBox<T extends FieldValues>({
	control,
	name,
	label,
	placeholder = '',
	options = [],
	readonly,
	disabled,
}: FormComboBoxProps<T>) {
	const [open, setOpen] = React.useState(false);

	return (
		<FormField
			control={control}
			name={name}
			render={({ field }) => {
				const selectedLabel = options.find(
					(option) => option.value === field.value,
				)?.label;

				return (
					<FormItem className='w-full flex-1'>
						{label && <FormLabel>{label}</FormLabel>}
						<Popover
							open={disabled ? false : open}
							onOpenChange={disabled ? undefined : setOpen}
						>
							<PopoverTrigger
								onClick={(e) => e.preventDefault()}
								disabled={disabled}
							>
								<FormControl>
									<InputGroup
										className={cn(
											'h-9 rounded-sm border-[#999999]',
											open && 'border-primary border-2',
											(readonly || disabled) && 'bg-transparent',
										)}
									>
										<InputGroupInput
											placeholder={placeholder}
											value={selectedLabel || ''}
											onClick={() => !disabled && setOpen(!open)}
											readOnly
										/>

										{field.value && !readonly && !disabled && (
											<InputGroupAddon
												align={'inline-end'}
												onClick={() => field.onChange('')}
												className='hover:text-error cursor-pointer'
											>
												<XIcon />
											</InputGroupAddon>
										)}

										{!disabled && (
											<InputGroupAddon
												align={'inline-end'}
												className='hover:text-primary cursor-pointer'
												onClick={() => setOpen(!open)}
											>
												<ChevronDownIcon />
											</InputGroupAddon>
										)}
									</InputGroup>
								</FormControl>
							</PopoverTrigger>

							<PopoverContent
								className='p-0'
								style={{ width: 'var(--radix-popover-trigger-width)' }}
								align='start'
							>
								<Command>
									<CommandInput placeholder={'Tìm kiếm'} />
									<CommandList
										className='max-h-58'
										onWheel={(e) => e.stopPropagation()}
									>
										<CommandEmpty>Không tìm thấy.</CommandEmpty>
										<CommandGroup>
											{options.map((option) => (
												<CommandItem
													key={`${option.label}-${option.value}`}
													value={`${option.label}-${option.value}`}
													onSelect={() => {
														field.onChange(option.value);
														setOpen(false);
													}}
												>
													{option.label}
													<Check
														className={cn(
															'ml-auto',
															field.value === option.value
																? 'opacity-100'
																: 'opacity-0',
														)}
													/>
												</CommandItem>
											))}
										</CommandGroup>
									</CommandList>
								</Command>
							</PopoverContent>
						</Popover>
						<FormMessage />
					</FormItem>
				);
			}}
		/>
	);
}
