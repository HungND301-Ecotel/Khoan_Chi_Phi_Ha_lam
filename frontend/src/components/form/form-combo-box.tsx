import { FormControlProps } from '@/components/form/form-provider';
import {
	QUICK_CREATE_VALUE,
	QuickCreateHost,
	QuickCreateOption,
	useQuickCreate,
} from '@/components/form/quick-create';
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
import { Check, ChevronDownIcon, PlusIcon, XIcon } from 'lucide-react';
import * as React from 'react';
import { FieldValues } from 'react-hook-form';

type CommonProps = {
	label?: string;
	placeholder?: string;
	options?: { value: string | number; label: string }[];
	readonly?: boolean;
	disabled?: boolean;
	/** Thêm mục "Thêm mới ..." để tạo nhanh bản ghi của danh mục khác */
	quickCreate?: QuickCreateOption;
};

function QuickCreateCommandItem({
	label,
	onSelect,
}: {
	label: string;
	onSelect: () => void;
}) {
	return (
		<CommandGroup>
			<CommandItem
				value={QUICK_CREATE_VALUE}
				onSelect={onSelect}
				className='text-primary data-[selected=true]:text-primary'
			>
				<PlusIcon className='text-primary' />
				Thêm mới {label.toLowerCase()}
			</CommandItem>
		</CommandGroup>
	);
}

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
	const [open, setOpenState] = React.useState(false);
	const [search, setSearch] = React.useState('');
	// Mục đang được chọn bằng bàn phím (điều khiển để Enter luôn chọn kết quả khớp đầu tiên)
	const [selected, setSelected] = React.useState('');
	const quickCreateState = useQuickCreate();

	// Đóng dropdown thì xoá từ khoá tìm kiếm và mục đang chọn
	const setOpen = (next: boolean) => {
		setOpenState(next);
		if (!next) {
			setSearch('');
			setSelected('');
		}
	};

	const {
		label,
		placeholder = '',
		options = [],
		readonly,
		disabled,
		quickCreate,
	} = props;
	const filterOptions = React.useCallback((value: string, search: string) => {
		if (value === QUICK_CREATE_VALUE) return 1;
		const normalizedValue = value.toLowerCase();
		const normalizedSearch = search.trim().toLowerCase();
		if (!normalizedSearch) return 1;
		return normalizedValue.includes(normalizedSearch) ? 1 : 0;
	}, []);

	// Mục "Thêm mới": đứng đầu danh sách, nhưng khi đang tìm kiếm thì xuống cuối
	// để Enter chọn kết quả tìm được thay vì mở form tạo mới
	const quickCreateItem = quickCreate && (
		<QuickCreateCommandItem
			label={quickCreate.label}
			onSelect={() => {
				setOpen(false);
				quickCreateState.start(options);
			}}
		/>
	);
	const isSearching = search.trim().length > 0;

	const handleSearchChange = (next: string) => {
		setSearch(next);
		const term = next.trim().toLowerCase();
		const match = term
			? options.find((option) =>
					`${option.label}-${option.value}`.toLowerCase().includes(term),
				)
			: undefined;
		if (match) setSelected(`${match.label}-${match.value}`);
		else if (term && quickCreate) setSelected(QUICK_CREATE_VALUE);
		else setSelected('');
	};

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
							filter={filterOptions}
							value={selected}
							onValueChange={setSelected}
						>
							<CommandInput
								placeholder={'Tìm kiếm'}
								value={search}
								onValueChange={handleSearchChange}
							/>
							<CommandList
								className='max-h-58'
								onWheel={(e) => e.stopPropagation()}
							>
								<CommandEmpty>Không tìm thấy.</CommandEmpty>
								{!isSearching && quickCreateItem}
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
								{isSearching && quickCreateItem}
							</CommandList>
						</Command>
					</PopoverContent>
				</Popover>
				{quickCreate && (
					<QuickCreateHost
						quickCreate={quickCreate}
						{...quickCreateState}
						options={options}
						onSelect={(value) => onValueChange(String(value))}
					/>
				)}
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
									filter={filterOptions}
									value={selected}
									onValueChange={setSelected}
								>
									<CommandInput
										placeholder={'Tìm kiếm'}
										value={search}
										onValueChange={handleSearchChange}
									/>
									<CommandList
										className='max-h-58'
										onWheel={(e) => e.stopPropagation()}
									>
										<CommandEmpty>Không tìm thấy.</CommandEmpty>
										{!isSearching && quickCreateItem}
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
										{isSearching && quickCreateItem}
									</CommandList>
								</Command>
							</PopoverContent>
						</Popover>
						{quickCreate && (
							<QuickCreateHost
								quickCreate={quickCreate}
								{...quickCreateState}
								options={options}
								onSelect={(value) => field.onChange(value)}
							/>
						)}
						<FormMessage />
					</FormItem>
				);
			}}
		/>
	);
}
