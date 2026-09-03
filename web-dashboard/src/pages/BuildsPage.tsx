import { useEffect, useState } from 'react'
import { buildsApi } from '../api/client'
import StatusBadge from '../components/StatusBadge'

export default function BuildsPage() {
  const [builds, setBuilds] = useState<any[]>([])
  const [logs, setLogs] = useState<any[]>([])
  const [selected, setSelected] = useState<string | null>(null)
  const [error, setError] = useState('')

  const load = async () => {
    try {
      const { data } = await buildsApi.list()
      setBuilds(Array.isArray(data) ? data : [])
    } catch {
      setError('Failed to load builds')
    }
  }

  useEffect(() => { load() }, [])

  const openLogs = async (id: string) => {
    setSelected(id)
    const { data } = await buildsApi.getLogs(id)
    setLogs(Array.isArray(data) ? data : [])
  }

  const cancel = async (id: string) => {
    try {
      await buildsApi.cancel(id)
      await load()
    } catch (err: any) {
      setError(err?.response?.data?.error || 'Cancel failed')
    }
  }

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Builds</h1>
        <button onClick={load} className="text-sm text-gray-400 hover:text-white">Refresh</button>
      </div>
      {error && <div className="bg-red-900/40 border border-red-700 text-red-300 rounded-lg p-3 text-sm">{error}</div>}
      <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="border-b border-gray-800 text-left text-xs uppercase text-gray-400">
              <th className="px-4 py-3">Build</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Created</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            {builds.map((b) => (
              <tr key={b.id} className="border-b border-gray-800">
                <td className="px-4 py-3 text-white">#{b.buildNumber}</td>
                <td className="px-4 py-3"><StatusBadge status={b.status} /></td>
                <td className="px-4 py-3 text-gray-400 text-sm">{new Date(b.createdAt).toLocaleString()}</td>
                <td className="px-4 py-3 text-right space-x-2">
                  <button onClick={() => openLogs(b.id)} className="text-xs bg-gray-700 px-3 py-1 rounded">Logs</button>
                  {(b.status === 'Pending' || b.status === 'Queued' || b.status === 'Running') && (
                    <button onClick={() => cancel(b.id)} className="text-xs bg-red-800 px-3 py-1 rounded">Cancel</button>
                  )}
                </td>
              </tr>
            ))}
            {builds.length === 0 && (
              <tr><td colSpan={4} className="px-4 py-10 text-center text-gray-500">No builds yet. Open a project IDE and click Build.</td></tr>
            )}
          </tbody>
        </table>
      </div>
      {selected && (
        <div className="bg-black border border-gray-800 rounded-xl p-4 font-mono text-xs text-gray-300 max-h-64 overflow-auto">
          {logs.length === 0 ? <span className="text-gray-600">No logs yet (worker is not running locally).</span> : logs.map((l, i) => (
            <div key={i}>{l.message}</div>
          ))}
        </div>
      )}
    </div>
  )
}
