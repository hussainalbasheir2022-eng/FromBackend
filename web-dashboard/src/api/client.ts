import axios from 'axios'

const BASE = import.meta.env.VITE_API_URL || ''

export const api = axios.create({
  baseURL: `${BASE}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
})

// Inject JWT token on every request
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Auto-redirect to login on 401
api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('accessToken')
      window.location.href = '/login'
    }
    return Promise.reject(err)
  }
)

// ─── Auth ────────────────────────────────────────────────────────────────────
export const authApi = {
  login: (email: string, password: string) =>
    api.post<{ accessToken: string; refreshToken: string; expiresAt: string }>(
      '/auth/login', { email, password }),
  register: (email: string, username: string, password: string) =>
    api.post('/auth/register', { email, username, password }),
}

// ─── Projects ─────────────────────────────────────────────────────────────────
export const projectsApi = {
  list: () => api.get('/projects'),
  get: (id: string) => api.get(`/projects/${id}`),
  create: (data: object) => api.post('/projects', data),
  update: (id: string, data: object) => api.put(`/projects/${id}`, data),
  delete: (id: string) => api.delete(`/projects/${id}`),
  build: (id: string) => api.post(`/projects/${id}/build`),
  publish: (id: string, data?: object) => api.post(`/projects/${id}/publish`, data ?? {}),
  downloadApk: async (id: string) => {
    const res = await api.get(`/projects/${id}/apk`, { responseType: 'blob', validateStatus: () => true })
    if (res.status !== 200) {
      let message = 'No APK yet. Publish first, then download.'
      const data = res.data
      if (data instanceof Blob) {
        try {
          const parsed = JSON.parse(await data.text())
          if (parsed?.error) message = parsed.error
        } catch { /* keep default */ }
      }
      throw new Error(message)
    }
    const disp = String(res.headers['content-disposition'] || '')
    const match = /filename\*?=(?:UTF-8''|"?)([^";]+)/i.exec(disp)
    const name = match ? decodeURIComponent(match[1].replace(/"/g, '')) : 'app.apk'
    const url = window.URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = name
    document.body.appendChild(a)
    a.click()
    a.remove()
    window.URL.revokeObjectURL(url)
  },
}

// ─── Files ────────────────────────────────────────────────────────────────────
export const filesApi = {
  list: (projectId: string) => api.get(`/projects/${projectId}/files`),
  get: (projectId: string, path: string) =>
    api.get(`/projects/${projectId}/files/${path}`),
  upsert: (projectId: string, path: string, content: string) =>
    api.put(`/projects/${projectId}/files/${path}`, { content }),
  delete: (projectId: string, path: string) =>
    api.delete(`/projects/${projectId}/files/${path}`),
}

// ─── Builds ──────────────────────────────────────────────────────────────────
export const buildsApi = {
  list: (projectId?: string) =>
    api.get('/builds', { params: projectId ? { projectId } : undefined }),
  get: (id: string) => api.get(`/builds/${id}`),
  getLogs: (id: string) => api.get(`/builds/${id}/logs`),
  cancel: (id: string) => api.post(`/builds/${id}/cancel`),
}

// ─── Releases ─────────────────────────────────────────────────────────────────
export const releasesApi = {
  list: (projectId?: string) =>
    api.get('/releases', { params: projectId ? { projectId } : undefined }),
  get: (id: string) => api.get(`/releases/${id}`),
  create: (data: object) => api.post('/releases', data),
  publish: (id: string, data: object) => api.post(`/releases/${id}/publish`, data),
  rollback: (id: string, reason?: string) =>
    api.post(`/releases/${id}/rollback`, { reason }),
}

// ─── Devices ─────────────────────────────────────────────────────────────────
export const devicesApi = {
  list: (params?: object) => api.get('/devices', { params }),
  get: (id: string) => api.get(`/devices/${id}`),
}

// ─── Updates ─────────────────────────────────────────────────────────────────
export const updatesApi = {
  latest: (applicationId: string, channel: string, currentVersion: string) =>
    api.get('/updates/latest', { params: { applicationId, channel, currentVersion } }),
}
