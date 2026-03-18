import {
	Command,
	CommandEmpty,
	CommandGroup,
	CommandInput,
	CommandItem,
	CommandList,
} from '@/components/ui/command';
import {
	InputGroup,
	InputGroupAddon,
	InputGroupInput,
} from '@/components/ui/input-group';
import { Label } from '@/components/ui/label';
import {
	Popover,
	PopoverContent,
	PopoverTrigger,
} from '@/components/ui/popover';
import { cn } from '@/lib/utils';
import { Check, ChevronDownIcon } from 'lucide-react';
import { useEffect, useState } from 'react';

export type ComboBoxProps = {
	label?: string;
	placeholder?: string;
	options?: { value: string; label: string }[];
	onChange?: (value: string) => void;
};

export function ComboBox({
	label,
	placeholder,
	options = [],
	onChange,
}: ComboBoxProps) {
	const [open, setOpen] = useState(false);
	const [title, setTitle] = useState('');
	const [value, setValue] = useState('');

	useEffect(() => {
		onChange?.(value);
	}, [value, onChange]);

	return (
		<div>
			{label && <Label>{label}</Label>}
			<Popover open={open} onOpenChange={setOpen}>
				<PopoverTrigger asChild>
					<InputGroup
						className={cn(
							'h-9 rounded-sm border-[#999999]',
							open && 'border-primary border-2',
						)}
					>
						<InputGroupInput
							placeholder={placeholder}
							value={title}
							onClick={() => setOpen(true)}
							readOnly
						/>
						<InputGroupAddon align={'inline-end'}>
							<ChevronDownIcon />
						</InputGroupAddon>
					</InputGroup>
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
										value={option.label}
										key={option.value}
										onSelect={() => {
											setValue(option.value);
											setTitle(option.label);
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
