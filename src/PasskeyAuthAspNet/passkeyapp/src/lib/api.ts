export const API_BASE_URL: string =
    import.meta.env.VITE_API_BASE_URL ??
    (() => {
        throw new Error('VITE_API_BASE_URL is not set in .env')
    })()

export function apiUrl(path: string): string {
    return `${API_BASE_URL}${path}`
}
