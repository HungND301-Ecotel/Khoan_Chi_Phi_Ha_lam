import { Button } from '@/components/ui/button';
import {
	Item,
	ItemActions,
	ItemContent,
	ItemDescription,
	ItemMedia,
	ItemTitle,
} from '@/components/ui/item';
import { cn } from '@/lib/utils';
import { ArrowLeftIcon, HomeIcon } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';

export function NotFound() {
	const navigate = useNavigate();

	return (
		<Item
			variant={'muted'}
			className='border-border mx-auto max-w-6xl justify-center gap-12 py-8 shadow'
		>
			<ItemContent className={cn('flex-none gap-5')}>
				<ItemTitle className='text-4xl font-semibold'>
					We looked everywhere.
				</ItemTitle>
				<ItemDescription className='text-foreground w-fit text-lg'>
					Looks like this page is missing. <br />
					If you still need help, visit our{' '}
					<Link to={'/helps'} className='font-medium'>
						help pages
					</Link>
					.
				</ItemDescription>
				<ItemActions className='gap-4'>
					<Button asChild size={'lg'} className='flex-1'>
						<Link to={`/`}>
							<HomeIcon />
							<span>Go to homepage</span>
						</Link>
					</Button>
					<Button
						size={'lg'}
						variant={'outline'}
						className='bg-muted hover:border-primary hover:text-primary flex-1'
						onClick={() => navigate(-1)}
					>
						<ArrowLeftIcon />
						<span>Go back</span>
					</Button>
				</ItemActions>
			</ItemContent>
			<ItemMedia className='my-auto flex-none'>
				<img
					src='/not-found.png'
					alt='page not found'
					className='h-96 object-contain'
				/>
			</ItemMedia>
		</Item>
	);
}
