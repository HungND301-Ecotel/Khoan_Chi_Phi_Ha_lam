import { flushSync } from 'react-dom';

/** Ô copy từ Excel/Sheets: tab giữa các cột, xuống dòng giữa các hàng. */
export function parsePastedGrid(text: string): string[][] {
	const lines = text.replace(/\r\n?/g, '\n').split('\n');
	// Excel luôn thêm 1 dấu xuống dòng ở cuối vùng copy.
	while (lines.length && !lines[lines.length - 1].trim()) lines.pop();
	return lines.map((line) =>
		line.split('\t').map((value) => value.replace(/\s+/g, ' ').trim()),
	);
}

/**
 * Số copy từ Excel theo kiểu ô nhập (dấu . ngăn nghìn, dấu , thập phân) hoặc kiểu en-US.
 * Ô trống là 0; chữ không đọc được thành số trả về undefined để giữ nguyên ô.
 */
export function parsePastedNumber(text: string): number | undefined {
	const raw = text.replace(/[\s₫đ%]/gi, '');
	if (!raw) return 0;
	const lastDot = raw.lastIndexOf('.');
	const lastComma = raw.lastIndexOf(',');
	let normalized: string;
	if (lastDot >= 0 && lastComma >= 0) {
		// Có cả hai dấu: dấu đứng sau cùng là dấu thập phân.
		normalized =
			lastComma > lastDot
				? raw.replace(/\./g, '').replace(',', '.')
				: raw.replace(/,/g, '');
	} else if (lastComma >= 0) {
		normalized =
			raw.indexOf(',') === lastComma
				? raw.replace(',', '.')
				: raw.replace(/,/g, '');
	} else if (lastDot >= 0) {
		// "1.234" / "1.234.567" là ngăn nghìn như ô nhập hiển thị; "1.5" là thập phân.
		const thousands =
			raw.indexOf('.') !== lastDot || /^-?\d{1,3}\.\d{3}$/.test(raw);
		normalized = thousands ? raw.replace(/\./g, '') : raw;
	} else {
		normalized = raw;
	}
	const value = Number(normalized);
	return Number.isFinite(value) ? value : undefined;
}

// Ô nhập số đang hiển thị → hàm ghi giá trị mới nhất của ô đó
const cellSetters = new WeakMap<HTMLInputElement, (value: number) => void>();

export function registerPasteCell(
	input: HTMLInputElement,
	setValue: (value: number) => void,
) {
	cellSetters.set(input, setValue);
}

type Cell = { input: HTMLInputElement; rect: DOMRect };

const centerY = (rect: DOMRect) => rect.top + rect.height / 2;
const sameRow = (a: DOMRect, b: DOMRect) =>
	Math.abs(centerY(a) - centerY(b)) < Math.min(a.height, b.height) / 2;

/**
 * Dán 1 khối ô copy từ Excel giống Hòn Gai: hàng i của khối vào hàng ô thứ i tính từ ô đang chọn,
 * cột j vào ô thứ j bên phải. Hàng và cột lấy theo vị trí hiển thị nên dùng được cho cả bảng lẫn
 * danh sách dòng; ô chỉ đọc vẫn giữ chỗ nhưng không bị ghi, ô vượt ra ngoài bị bỏ qua.
 * Trả về false khi chỉ dán 1 giá trị để ô nhập tự xử lý như bình thường.
 */
export function spreadPasteFrom(start: HTMLInputElement, text: string): boolean {
	const grid = parsePastedGrid(text);
	if (grid.length <= 1 && (grid[0]?.length ?? 0) <= 1) return false;

	// Trong bảng thì chỉ rải trong bảng đó, ngoài ra rải trong cả hộp thoại/trang.
	const scope =
		start.closest('table') ?? start.closest('[role="dialog"]') ?? document.body;
	const cells: Cell[] = Array.from(scope.querySelectorAll('input'))
		.filter((input) => cellSetters.has(input))
		.map((input) => ({ input, rect: input.getBoundingClientRect() }))
		.filter(({ rect }) => rect.width > 0 && rect.height > 0);
	const origin = cells.find((cell) => cell.input === start);
	if (!origin) return false;

	// Cột: ô đang chọn và các ô bên phải nó trên cùng hàng.
	const columns = cells
		.filter(
			(cell) =>
				sameRow(cell.rect, origin.rect) && cell.rect.left >= origin.rect.left - 1,
		)
		.sort((a, b) => a.rect.left - b.rect.left)
		.slice(0, Math.max(...grid.map((line) => line.length)))
		.map((cell) => cell.rect.left + cell.rect.width / 2);
	const inColumn = (rect: DOMRect, x: number) =>
		rect.left <= x && x <= rect.right;

	// Hàng: các hàng từ ô đang chọn trở xuống có ô thẳng cột với các cột trên.
	const rows: Cell[][] = [];
	cells
		.filter(
			(cell) =>
				centerY(cell.rect) >= centerY(origin.rect) - origin.rect.height / 2 &&
				columns.some((x) => inColumn(cell.rect, x)),
		)
		.sort((a, b) => centerY(a.rect) - centerY(b.rect))
		.forEach((cell) => {
			const row = rows[rows.length - 1];
			if (row && sameRow(row[0].rect, cell.rect)) row.push(cell);
			else rows.push([cell]);
		});

	const writes: { input: HTMLInputElement; value: number }[] = [];
	grid.forEach((line, i) => {
		const row = rows[i];
		if (!row) return;
		line.forEach((text, j) => {
			const x = columns[j];
			const cell = x === undefined ? undefined : row.find((c) => inColumn(c.rect, x));
			const value = parsePastedNumber(text);
			if (!cell || value === undefined) return;
			if (cell.input.disabled || cell.input.readOnly) return;
			writes.push({ input: cell.input, value });
		});
	});

	// Ghi lần lượt từng ô và render ngay sau mỗi ô, để hàm cập nhật của ô kế tiếp
	// đọc được dữ liệu đã có giá trị của ô trước.
	writes.forEach(({ input, value }) => {
		flushSync(() => cellSetters.get(input)?.(value));
	});
	return true;
}
