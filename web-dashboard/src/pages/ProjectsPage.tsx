import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { projectsApi } from '../api/client'

export default function ProjectsPage() {
  const [projects, setProjects] = useState<any[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [apkBusy, setApkBusy] = useState<string | null>(null)
  const navigate = useNavigate()

  const load = async () => {
    setLoading(true)
    try {
      const { data } = await projectsApi.list()
      setProjects(Array.isArray(data) ? data : [])
      setError('')
    } catch (err: any) {
      setError(err?.response?.data?.error || 'Failed to load projects')
    } finally {
      setLoading(false)
    }
  }

  const downloadApk = async (id: string) => {
    setApkBusy(id)
    setError('')
    try {
      await projectsApi.downloadApk(id)
    } catch (err: any) {
      setError(err?.message || err?.response?.data?.error || 'Download failed. Publish first to create an APK.')
    } finally {
      setApkBusy(null)
    }
  }

  useEffect(() => { load() }, [])

  const remove = async (id: string) => {
    if (!confirm('Delete this project?')) return
    try {
      await projectsApi.delete(id)
      await load()
    } catch (err: any) {
      setError(err?.response?.data?.error || 'Delete failed (admin role required)')
    }
  }

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Projects</h1>
        <button
          onClick={() => navigate('/projects/new')}
          className="bg-blue-600 hover:bg-blue-700 text-white text-sm px-4 py-2 rounded-lg"
        >
          New Project
        </button>
      </div>
      {error && <div className="bg-red-900/40 border border-red-700 text-red-300 rounded-lg p-3 text-sm">{error}</div>}
      <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading...</p>
        ) : projects.length === 0 ? (
          <p className="p-6 text-gray-500 text-center">
            No projects yet.{' '}
            <button className="text-blue-400 hover:underline" onClick={() => navigate('/projects/new')}>Create one</button>
          </p>
        ) : (
          <table className="w-full">
            <thead>
              <tr className="border-b border-gray-800 text-left text-xs uppercase text-gray-400">
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Package</th>
                <th className="px-4 py-3">Version</th>
                <th className="px-4 py-3">Build #</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody>
              {projects.map((p) => (
                <tr key={p.id} className="border-b border-gray-800 hover:bg-gray-800/40">
                  <td className="px-4 py-3 text-white font-medium">{p.name}</td>
                  <td className="px-4 py-3 text-gray-400 text-sm">{p.applicationId}</td>
                  <td className="px-4 py-3 text-gray-300 text-sm">v{p.version}</td>
                  <td className="px-4 py-3 text-gray-300 text-sm">{p.buildNumber}</td>
                  <td className="px-4 py-3 text-right space-x-2">
                    <Link to={`/ide/${p.id}`} className="text-xs bg-blue-600 hover:bg-blue-700 px-3 py-1 rounded">Open IDE</Link>
                    <button
                      onClick={() => downloadApk(p.id)}
                      disabled={apkBusy === p.id}
                      className="text-xs bg-amber-700 hover:bg-amber-600 disabled:opacity-50 px-3 py-1 rounded"
                    >
                      {apkBusy === p.id ? '…' : 'Share APK'}
                    </button>
                    <button onClick={() => remove(p.id)} className="text-xs bg-gray-700 hover:bg-red-800 px-3 py-1 rounded">Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
