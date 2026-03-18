import { NotFound } from '@/components/not-found';
import { Container } from '@/components/ui/container';

function NotFoundPage() {
	return (
		<div className='flex h-screen w-screen items-center justify-center'>
			<Container>
				<NotFound />
			</Container>
		</div>
	);
}

export default NotFoundPage;
