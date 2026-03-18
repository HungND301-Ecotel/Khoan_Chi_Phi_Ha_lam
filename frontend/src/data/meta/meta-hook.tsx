import { useMatches } from 'react-router-dom';

export type Meta = {
	title?: string;
	breadcrumb?: string;
};

export function useMeta() {
	const matches = useMatches();

	const handles = matches
		.filter((match) => match.handle)
		.map((match) => match.handle as Meta);

	const breadcrumbs = handles
		.filter((handle) => handle.breadcrumb)
		.map((handle) => handle.breadcrumb);
	const breadcrumb = breadcrumbs[breadcrumbs.length - 1];

	const titles = handles
		.filter((handle) => handle.title)
		.map((handle) => handle.title);
	const title = titles[titles.length - 1];

	return { title, breadcrumbs, breadcrumb };
}
