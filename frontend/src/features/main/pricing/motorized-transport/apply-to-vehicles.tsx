import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from '@/components/ui/dialog';
import { CopyPlus } from 'lucide-react';
import { useState } from 'react';

type ApplyToVehiclesButtonProps = {
	sourceId: string;
	/** Các xe đang chọn trong khoảng thời gian (gồm cả xe nguồn) */
	vehicleIds: string[];
	getTitle: (vehicleId: string) => string;
	onApply: (targetIds: string[]) => void;
	/** Nhãn nút / tiêu đề hộp thoại (mặc định: Áp dụng cho Nhóm VT, TS khác) */
	label?: string;
	/** Tên đối tượng đích hiển thị trong hộp thoại (mặc định: Nhóm VT, TS) */
	targetNoun?: string;
	/** Phần dữ liệu được chép (mặc định: công đoạn, thông số và đơn giá) */
	copiedContent?: string;
};

/** Nút "Áp dụng cho ... khác" trên tiêu đề khối (xe, loại hàng...) để chép cấu hình sang khối cùng loại */
export function ApplyToVehiclesButton({
	sourceId,
	vehicleIds,
	getTitle,
	onApply,
	label = 'Áp dụng cho Nhóm VT, TS khác',
	targetNoun = 'Nhóm VT, TS',
	copiedContent = 'công đoạn, thông số và đơn giá',
}: ApplyToVehiclesButtonProps) {
	const others = vehicleIds.filter((id) => id !== sourceId);
	const [open, setOpen] = useState(false);
	const [selected, setSelected] = useState<string[]>([]);

	if (others.length === 0) return null;

	const allSelected = selected.length === others.length;

	return (
		<>
			<Button
				type='button'
				variant='ghost'
				size='sm'
				className='text-primary hover:text-primary h-8 gap-1.5 bg-transparent px-2 shadow-none hover:bg-neutral-100 hover:shadow-none'
				onClick={() => {
					setSelected(others);
					setOpen(true);
				}}
			>
				<CopyPlus className='size-4' />
				{label}
			</Button>

			<Dialog open={open} onOpenChange={setOpen}>
				<DialogContent className='sm:max-w-lg'>
					<DialogHeader>
						<DialogTitle>{label}</DialogTitle>
						<DialogDescription>
							Chép {copiedContent} của{' '}
							<span className='font-semibold text-black'>
								{getTitle(sourceId)}
							</span>{' '}
							sang các {targetNoun} được chọn. Dữ liệu đang nhập của các {targetNoun}{' '}
							này sẽ bị ghi đè.
						</DialogDescription>
					</DialogHeader>

					<label className='flex cursor-pointer items-center gap-2 border-b pb-2 text-sm font-medium'>
						<Checkbox
							checked={allSelected}
							onCheckedChange={(checked) => setSelected(checked ? others : [])}
						/>
						Chọn tất cả ({others.length} {targetNoun})
					</label>

					<div className='flex max-h-72 flex-col gap-2 overflow-y-auto'>
						{others.map((id) => (
							<label
								key={id}
								className='flex cursor-pointer items-center gap-2 text-sm'
							>
								<Checkbox
									checked={selected.includes(id)}
									onCheckedChange={(checked) =>
										setSelected((prev) =>
											checked
												? [...prev, id]
												: prev.filter((value) => value !== id),
										)
									}
								/>
								{getTitle(id)}
							</label>
						))}
					</div>

					<DialogFooter>
						<Button
							type='button'
							variant='outline'
							className='h-8 w-24'
							onClick={() => setOpen(false)}
						>
							Huỷ
						</Button>
						<Button
							type='button'
							className='h-8'
							disabled={selected.length === 0}
							onClick={() => {
								onApply(selected);
								setOpen(false);
							}}
						>
							Áp dụng ({selected.length})
						</Button>
					</DialogFooter>
				</DialogContent>
			</Dialog>
		</>
	);
}
