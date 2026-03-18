import type { FormControlProps } from '@/components/form/form-provider';
import { Calendar } from '@/components/ui/calendar';
import {
	FormControl,
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
import { format, parse, parseISO } from 'date-fns';
import { vi } from 'date-fns/locale';
import { CalendarIcon } from 'lucide-react';
import type React from 'react';
import { useState } from 'react';
import type { ControllerRenderProps, FieldValues } from 'react-hook-form';
import { useController } from 'react-hook-form';

export type FormDateProps<T extends FieldValues> = FormControlProps<T> & {
	placeholder?: string;
	readonly?: boolean;
};

function parseISODate(value: unknown): Date | null {
	if (!value || typeof value !== 'string') return null;
	try {
		const date = parseISO(value);
		return isNaN(date.getTime()) ? null : date;
	} catch {
		return null;
	}
}

function toISODateString(date: Date): string {
	return format(date, 'yyyy-MM-dd');
}

function FormDateInput<T extends FieldValues>({
	field,
	placeholder,
	readonly,
}: {
	field: ControllerRenderProps<T>;
	placeholder?: string;
	readonly?: boolean;
}) {
	const [inputValue, setInputValue] = useState<string | null>(null);
	const [open, setOpen] = useState(false);

	const parsedDate = parseISODate(field.value);
	const displayValue =
		inputValue !== null
			? inputValue
			: parsedDate
				? format(parsedDate, 'dd/MM/yyyy', { locale: vi })
				: '';

	const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
		const value = e.target.value;
		setInputValue(value);

		if (value.length === 10) {
			const parsed = parse(value, 'dd/MM/yyyy', new Date(), { locale: vi });
			if (!isNaN(parsed.getTime())) {
				field.onChange(toISODateString(parsed));
			}
		}
	};

	const handleInputFocus = () => {
		if (inputValue === null) {
			setInputValue(
				parsedDate ? format(parsedDate, 'dd/MM/yyyy', { locale: vi }) : '',
			);
		}
	};

	const handleInputBlur = () => {
		if (inputValue) {
			const parsed = parse(inputValue, 'dd/MM/yyyy', new Date(), {
				locale: vi,
			});
			if (!isNaN(parsed.getTime())) {
				field.onChange(toISODateString(parsed));
			}
		}
		setInputValue(null);
	};

	const handleCalendarSelect = (date: Date | undefined) => {
		setInputValue(null);
		field.onChange(date ? toISODateString(date) : undefined);
		setOpen(false);
	};

	return (
		<InputGroup className={cn(readonly && 'bg-transparent')}>
			<InputGroupInput
				placeholder={placeholder ?? 'dd/MM/yyyy'}
				value={displayValue}
				onChange={handleInputChange}
				onFocus={handleInputFocus}
				onBlur={handleInputBlur}
				readOnly={readonly}
			/>
			<InputGroupAddon align={'inline-end'}>
				<Popover open={open} onOpenChange={setOpen}>
					<PopoverTrigger className='cursor-pointer' disabled={readonly}>
						<CalendarIcon className='me-2 size-3.5' strokeWidth={2} />
					</PopoverTrigger>
					<PopoverContent
						align='end'
						alignOffset={-8}
						sideOffset={10}
						className='mb-2 w-auto border-[#999999] p-0 shadow-none'
					>
						<Calendar
							mode='single'
							locale={vi}
							selected={parsedDate ?? undefined}
							onSelect={handleCalendarSelect}
						/>
					</PopoverContent>
				</Popover>
			</InputGroupAddon>
		</InputGroup>
	);
}

export function FormDate<T extends FieldValues>({
	control,
	name,
	label,
	placeholder,
	readonly,
}: FormDateProps<T>) {
	const { field, fieldState } = useController({ control, name });

	return (
		<FormItem className='w-full flex-1'>
			<FormLabel>{label}</FormLabel>
			<FormControl>
				<FormDateInput
					field={field}
					placeholder={placeholder}
					readonly={readonly}
				/>
			</FormControl>
			{fieldState.error && (
				<FormMessage>{fieldState.error.message}</FormMessage>
			)}
		</FormItem>
	);
}
