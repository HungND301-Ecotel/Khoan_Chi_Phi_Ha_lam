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

type CommonProps = {
	label?: string;
	placeholder?: string;
	options?: { value: string | number; label: string }[];
	readonly?: boolean;
	disabled?: boolean;
};

// Controlled mode: gắn với react-hook-form
type ControlledProps<T extends FieldValues> = CommonProps &
	FormControlProps<T> & {
		value?: never;
		onValueChange?: never;
	};

// Uncontrolled mode: dùng value/onValueChange trực tiếp (không cần form context)
type UncontrolledProps = CommonProps & {
	control?: never;
	name?: never;
	value: string;
	onValueChange: (value: string) => void;
};

export type FormComboBoxProps<T extends FieldValues> =
	| ControlledProps<T>
	| UncontrolledProps;

export function FormComboBox<T extends FieldValues>(
	props: FormComboBoxProps<T>,
) {
	const [open, setOpen] = React.useState(false);

	const { label, placeholder = '', options = [], readonly, disabled } = props;

	// Uncontrolled mode
	if (props.control === undefined || props.control === null) {
		const { value, onValueChange } = props as UncontrolledProps;
		const selectedLabel = options.find((o) => o.value === value)?.label;

		return (
			<div className='flex w-full flex-1 flex-col gap-2'>
				{label && (
					<label className='text-sm leading-none font-medium peer-disabled:cursor-not-allowed peer-disabled:opacity-70'>
						{label}
					</label>
				)}
				<Popover
					open={disabled ? false : open}
					onOpenChange={disabled ? undefined : setOpen}
				>
					<PopoverTrigger
						onClick={(e) => e.preventDefault()}
						disabled={disabled}
						className='w-full'
					>
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

							{value && !readonly && !disabled && (
								<InputGroupAddon
									align={'inline-end'}
									onClick={() => onValueChange('')}
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
					</PopoverTrigger>

					<PopoverContent
						className='p-0'
						style={{ width: 'var(--radix-popover-trigger-width)' }}
						align='start'
					>
						<Command
							filter={(value, search) => {
								const labelPart = value.split('-')[0].toLowerCase();
								const normalizedSearch = search.toLowerCase();
								if (labelPart.includes(normalizedSearch)) return 1;
								return 0;
							}}
						>
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
												onValueChange(String(option.value));
												setOpen(false);
											}}
										>
											{option.label}
											<Check
												className={cn(
													'ml-auto',
													value === option.value ? 'opacity-100' : 'opacity-0',
												)}
											/>
										</CommandItem>
									))}
								</CommandGroup>
							</CommandList>
						</Command>
					</PopoverContent>
				</Popover>
			</div>
		);
	}

	// Controlled mode (react-hook-form) — giữ nguyên như cũ
	const { control, name } = props as ControlledProps<T>;

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
								<Command
									filter={(value, search) => {
										const labelPart = value.split('-')[0].toLowerCase();
										const normalizedSearch = search.toLowerCase();
										if (labelPart.includes(normalizedSearch)) return 1;
										return 0;
									}}
								>
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
