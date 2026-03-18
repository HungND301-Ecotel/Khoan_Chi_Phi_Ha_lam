import { useMeta } from '@/data/meta/meta-hook';

export interface MatchHandle {
	breadcrumb?: string;
}

interface DynamicTitleProps {
	prefix?: string;
	suffix?: string;
}

export function DynamicTitle({ prefix, suffix }: DynamicTitleProps) {
	const { title } = useMeta();
	return (
		<h4 className='text-[2.125rem] leading-tight font-normal text-[#2b4a82]'>
			{prefix} {title} {suffix}
		</h4>
	);
}
