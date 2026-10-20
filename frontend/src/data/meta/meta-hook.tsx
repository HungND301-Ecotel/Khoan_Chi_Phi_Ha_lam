import { createContext, useContext } from 'react';
import { useMatches } from 'react-router-dom';

export type Meta = {
	title?: string;
	breadcrumb?: string;
};

/**
 * Ghi đè breadcrumb hiện tại cho một nhánh component (vd: form tạo nhanh
 * danh mục khác mở từ dropdown) để tiêu đề và thông báo dùng đúng tên danh mục.
 */
export const MetaBreadcrumbOverrideContext = createContext<string | undefined>(
	undefined,
);

export function useMeta() {
	const matches = useMatches();
	const breadcrumbOverride = useContext(MetaBreadcrumbOverrideContext);

	const handles = matches
		.filter((match) => match.handle)
		.map((match) => match.handle as Meta);

	const breadcrumbs = handles
		.filter((handle) => handle.breadcrumb)
		.map((handle) => handle.breadcrumb);
	const breadcrumb = breadcrumbOverride ?? breadcrumbs[breadcrumbs.length - 1];

	const titles = handles
		.filter((handle) => handle.title)
		.map((handle) => handle.title);
	const title = titles[titles.length - 1];

	return { title, breadcrumbs, breadcrumb };
}
