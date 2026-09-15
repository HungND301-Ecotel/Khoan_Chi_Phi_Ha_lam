import * as XLSX from 'xlsx';

export function exportMaterialTemplate(daysInMonth: number = 31): void {
	const workbook = XLSX.utils.book_new();

	for (let day = 1; day <= daysInMonth; day++) {
		const sheetName = String(day).padStart(2, '0');
		const data: Array<Record<string, string>> = [];

		const worksheet = XLSX.utils.json_to_sheet(data, {
			header: [
				'Id',
				'Số chứng từ',
				'Ngày vào sổ',
				'Mã vật tư',
				'Số lượng lĩnh',
				'Số lượng xuất',
			],
		});

		worksheet['!cols'] = [
			{ hidden: true }, // Ẩn cột Id (Cột A)
			{ wch: 18 }, // Cột Số chứng từ (Cột B)
			{ wch: 15 }, // Cột Ngày vào sổ (Cột C)
			{ wch: 20 }, // Cột Mã vật tư (Cột D)
			{ wch: 15 }, // Cột Số lượng lĩnh (Cột E)
			{ wch: 15 }, // Cột Số lượng xuất (Cột F)
		];

		XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);
	}

	const fileName = `mau_bbnt_${new Date().getTime()}.xlsx`;
	XLSX.writeFile(workbook, fileName);
}
