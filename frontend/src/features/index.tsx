import { AuthRouter } from '@/features/auth/router';
import { RootLayout } from '@/features/layout';
import MainRouter from '@/features/main/router';
import NotFoundPage from '@/features/not-found';
import { createBrowserRouter, RouteObject } from 'react-router-dom';

export const features: RouteObject = {
	element: <RootLayout />,
	children: [
		AuthRouter,
		MainRouter,
		{
			path: '*',
			element: <NotFoundPage />,
		},
	],
};

export const router = createBrowserRouter([features]);
