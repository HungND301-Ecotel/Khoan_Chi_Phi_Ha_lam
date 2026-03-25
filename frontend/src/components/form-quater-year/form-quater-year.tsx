import { FormControlProps } from '@/components/form/form';
import { Button } from '@/components/ui/button';
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Field, FieldError, FieldLabel } from '@/components/ui/field';
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';
import { CalendarIcon } from 'lucide-react';
import { useState } from 'react';
import { FieldValues, Path, useController } from 'react-hook-form';

export type FormQuaterYearProps<T extends FieldValues> = Omit<
	FormControlProps<T>,
	'name'
> & {
	quarter: Path<T>;
	year: Path<T>;
	placeholder?: string;
	required?: boolean;
	disabled?: boolean;
};

export function FormQuaterYear<T extends FieldValues>({
	control,
	quarter,
	year,
	label,
	placeholder = 'Q/yyyy',
	required,
	disabled,
}: FormQuaterYearProps<T>) {
	const { field: quarterField, fieldState: quarterFieldState } = useController({
		control,
		name: quarter,
		disabled,
	});

	const { field: yearField, fieldState: yearFieldState } = useController({
		control,
		name: year,
		disabled,
	});

	const currentYear = new Date().getFullYear();
	const [selectedYear, setSelectedYear] = useState<number>(
		yearField.value ? Number(yearField.value) : currentYear,
	);
	const [isOpen, setIsOpen] = useState(false);

	const displayValue =
		quarterField.value && yearField.value
			? `Quý ${quarterField.value}/${yearField.value}`
			: placeholder;

	const handleYearChange = (yearStr: string) => {
		const yearNum = Number(yearStr);
		setSelectedYear(yearNum);
		if (quarterField.value) {
			yearField.onChange(yearStr);
		}
	};

	const handleQuarterClick = (quarterNum: number) => {
		quarterField.onChange(String(quarterNum));
		yearField.onChange(String(selectedYear));
		setIsOpen(false);
	};

	const handleOpenChange = (open: boolean) => {
		setIsOpen(open);
		if (open) {
			if (!yearField.value) {
				setSelectedYear(currentYear);
			} else {
				setSelectedYear(Number(yearField.value));
			}
		}
	};

	return (
		<Field
			className='grid flex-1 gap-1'
			data-invalid={quarterFieldState.invalid || yearFieldState.invalid}
		>
			{label && (
				<FieldLabel>
					<span>{label}</span>
					{required && <span className='text-destructive'> *</span>}
				</FieldLabel>
			)}
			<DropdownMenu open={isOpen} onOpenChange={handleOpenChange}>
				<DropdownMenuTrigger asChild>
					<Button
						size={'lg'}
						variant={'outline'}
						className='hover:bg-background h-9 w-full justify-between'
						disabled={disabled}
					>
						<span
							className={cn(
								'truncate font-normal',
								displayValue === placeholder && 'text-muted-foreground',
							)}
						>
							{displayValue}
						</span>
						<CalendarIcon className='h-4 w-4 opacity-50' />
					</Button>
				</DropdownMenuTrigger>

				<DropdownMenuContent className='w-[280px] space-y-3 p-3' align='start'>
					<Select value={String(selectedYear)} onValueChange={handleYearChange}>
						<SelectTrigger className='w-full'>
							<SelectValue placeholder='Chọn năm' />
						</SelectTrigger>
						<SelectContent className='max-h-[200px]'>
							{Array.from({ length: 201 }, (_, index) => {
								const y = currentYear + 100 - index;
								return (
									<SelectItem key={y} value={String(y)}>
										{y}
									</SelectItem>
								);
							})}
						</SelectContent>
					</Select>

					<div className='grid grid-cols-2 gap-2'>
						{[1, 2, 3, 4].map((q) => {
							const isSelected = Number(quarterField.value) === q;
							return (
								<Button
									variant={isSelected ? 'default' : 'ghost'}
									size='sm'
									key={q}
									onClick={() => handleQuarterClick(q)}
									className='h-9'
								>
									{q}
								</Button>
							);
						})}
					</div>
				</DropdownMenuContent>
			</DropdownMenu>
			{(quarterFieldState.invalid || yearFieldState.invalid) && (
				<FieldError errors={[quarterFieldState.error, yearFieldState.error]} />
			)}
		</Field>
	);
}
