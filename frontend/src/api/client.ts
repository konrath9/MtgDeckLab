import axios from 'axios'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5052/api'

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
})

const TOKEN_STORAGE_KEY = 'mtgdecklab.token'

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_STORAGE_KEY)
}

export function setStoredToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_STORAGE_KEY, token)
  else localStorage.removeItem(TOKEN_STORAGE_KEY)
}

apiClient.interceptors.request.use((config) => {
  const token = getStoredToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

export const UNAUTHORIZED_EVENT = 'auth:unauthorized'

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401 && getStoredToken()) {
      setStoredToken(null)
      window.dispatchEvent(new Event(UNAUTHORIZED_EVENT))
    }
    return Promise.reject(error)
  },
)

export function extractErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { error?: string } | undefined
    return data?.error ?? fallback
  }
  return fallback
}
