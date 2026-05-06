import { FormControlProps } from '@/components/form/form-provider';
import { FieldError } from '@/components/ui/field';
import { InputGroup, InputGroupInput } from '@/components/ui/input-group';
import { Label } from '@/components/ui/label';
import { cn, formatNumber } from '@/lib/utils';
import { ComponentProps } from 'react';
import {
	ControllerRenderProps,
	FieldValues,
	Path,
	useController,
} from 'react-hook-form';
import { NumericFormat } from 'react-number-format';

export type FormNumberProps<T extends FieldValues> = FormControlProps<T> &
	Omit<
		ComponentProps<'input'>,
		| keyof FormControlProps<T>
		| keyof ControllerRenderProps<T, Path<T>>
		| 'id'
		| 'type'
		| 'inputMode'
		| 'pattern'
		| 'defaultValue'
	> & {
		disabled?: boolean;
	};

export type FormNumberInputProps = Omit<
	ComponentProps<'input'>,
	'id' | 'type' | 'inputMode' | 'pattern' | 'defaultValue'
> & {
	value?: number;
	onValueChange?: (value?: number) => void;
	className?: string;
	disabled?: boolean;
};

export function FormNumber<T extends FieldValues>({
	control,
	name,
	label,
	required,
	className,
	autoComplete = 'off',
	disabled,
	...props
}: FormNumberProps<T>) {
	const {
		field: { onChange, value, ...field },
		fieldState,
	} = useController({ control, name });
	const numberTitle =
		(props.readOnly || disabled) &&
		value !== undefined &&
		value !== null &&
		value !== ''
			? formatNumber(Number(value))
			: props.title;

	return (
		<div data-invalid={fieldState.invalid} className='flex flex-col gap-2'>
			{label && (
				<Label htmlFor={name}>
					<span>{label}</span>
					{required && <span className='text-destructive'> *</span>}
				</Label>
			)}
			<InputGroup className={cn(className, disabled && 'bg-transparent')}>
				<NumericFormat
					decimalSeparator=','
					thousandSeparator='.'
					value={value}
					onValueChange={(values) => {
						onChange(values.floatValue);
					}}
					{...field}
					customInput={InputGroupInput}
					id={name}
					autoComplete={autoComplete}
					required={required}
					title={numberTitle}
					type='text'
					inputMode='decimal'
					disabled={disabled}
					{...props}
				/>
			</InputGroup>
			{fieldState.invalid && <FieldError errors={[fieldState.error]} />}
		</div>
	);
}

export function FormNumberInput({
	value,
	onValueChange,
	className,
	autoComplete = 'off',
	disabled,
	...props
}: FormNumberInputProps) {
	const numberTitle =
		(props.readOnly || disabled) && value !== undefined && value !== null
			? formatNumber(value)
			: props.title;

	return (
		<InputGroup className={cn(className, disabled && 'bg-transparent')}>
			<NumericFormat
				decimalSeparator=','
				thousandSeparator='.'
				value={value}
				onValueChange={(values) => {
					onValueChange?.(values.floatValue);
				}}
				customInput={InputGroupInput}
				autoComplete={autoComplete}
				title={numberTitle}
				type='text'
				inputMode='decimal'
				disabled={disabled}
				{...props}
			/>
		</InputGroup>
	);
}
