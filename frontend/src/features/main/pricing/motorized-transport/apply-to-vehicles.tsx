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
};

/** Nút "Áp dụng cho các xe khác" trên tiêu đề khối của một xe */
export function ApplyToVehiclesButton({
	sourceId,
	vehicleIds,
	getTitle,
	onApply,
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
				Áp dụng cho các xe khác
			</Button>

			<Dialog open={open} onOpenChange={setOpen}>
				<DialogContent className='sm:max-w-lg'>
					<DialogHeader>
						<DialogTitle>Áp dụng cho các xe khác</DialogTitle>
						<DialogDescription>
							Chép công đoạn, thông số và đơn giá của{' '}
							<span className='font-semibold text-black'>
								{getTitle(sourceId)}
							</span>{' '}
							sang các xe được chọn. Dữ liệu đang nhập của các xe này sẽ bị ghi
							đè.
						</DialogDescription>
					</DialogHeader>

					<label className='flex cursor-pointer items-center gap-2 border-b pb-2 text-sm font-medium'>
						<Checkbox
							checked={allSelected}
							onCheckedChange={(checked) => setSelected(checked ? others : [])}
						/>
						Chọn tất cả ({others.length} xe)
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
