import { PopupProvider } from '@/components/popup';
import { ThemeProvider } from '@/data/theme/theme-provider';
import { router } from '@/features';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import './index.css';

createRoot(document.getElementById('root')!).render(
	<StrictMode>
		<ThemeProvider defaultTheme='light' storageKey='vite-ui-theme'>
			<PopupProvider>
				<RouterProvider router={router} />
			</PopupProvider>
		</ThemeProvider>
	</StrictMode>,
);
