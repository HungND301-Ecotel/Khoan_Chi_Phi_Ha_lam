// In development the Vite proxy keeps auth cookies on the frontend origin.
export const apiBase = import.meta.env.DEV
	? '/api'
	: (import.meta.env.VITE_API_BASE_URL || '/api');
