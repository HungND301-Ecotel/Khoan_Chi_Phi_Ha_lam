import * as XLSX from 'xlsx';
import { MaterialImportRow } from './types';

export async function parseExcelMaterialFile(
	file: File,
): Promise<MaterialImportRow[]> {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();

		reader.onload = (event) => {
			try {
				const data = event.target?.result;
				if (!data) {
					throw new Error('Không thể đọc file');
				}

				// Parse workbook
				const workbook = XLSX.read(data, { type: 'binary' });

				// Get first sheet
				const sheetName = workbook.SheetNames[0];
				if (!sheetName) {
					throw new Error('File Excel không chứa sheet nào');
				}

				const sheet = workbook.Sheets[sheetName];
				if (!sheet) {
					throw new Error('Không thể đọc sheet');
				}

				// Convert sheet to JSON
				const sheetData = XLSX.utils.sheet_to_json<
					Record<string, string | number>
				>(sheet, {
					defval: '',
				});

				if (sheetData.length === 0) {
					throw new Error('Sheet không chứa dữ liệu');
				}

				// Find column headers (case-insensitive)
				const headers = Object.keys(sheetData[0] || {});
				const materialCodeColumn = headers.find(
					(h) =>
						h.toLowerCase().includes('mã') && h.toLowerCase().includes('vật'),
				);
				const quantityReceivedColumn = headers.find((h) =>
					h.toLowerCase().includes('lĩnh'),
				);
				const quantityExportedColumn = headers.find((h) =>
					h.toLowerCase().includes('xuất'),
				);

				if (
					!materialCodeColumn ||
					!quantityReceivedColumn ||
					!quantityExportedColumn
				) {
					throw new Error(
						'File Excel phải chứa 3 cột: "Mã vật tư", "Số lượng lĩnh", "Số lượng xuất"',
					);
				}

				// Map data to MaterialImportRow
				const result: MaterialImportRow[] = sheetData
					.filter((row: Record<string, string | number>) => {
						const code = String(row[materialCodeColumn] || '').trim();
						const qtyReceived = Number(row[quantityReceivedColumn]);
						const qtyExported = Number(row[quantityExportedColumn]);
						return code && !isNaN(qtyReceived) && !isNaN(qtyExported);
					})
					.map((row: Record<string, string | number>) => ({
						materialCode: String(row[materialCodeColumn] || '').trim(),
						quantityReceived: Number(row[quantityReceivedColumn]) || 0,
						quantityExported: Number(row[quantityExportedColumn]) || 0,
					}));

				if (result.length === 0) {
					throw new Error('File Excel không chứa dữ liệu hợp lệ');
				}

				resolve(result);
			} catch (error) {
				reject(error);
			}
		};

		reader.onerror = () => {
			reject(new Error('Lỗi khi đọc file'));
		};

		reader.readAsArrayBuffer(file);
	});
}
