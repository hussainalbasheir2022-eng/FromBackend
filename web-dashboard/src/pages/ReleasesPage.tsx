import { useEffect, useState } from 'react'
import { buildsApi, projectsApi, releasesApi } from '../api/client'
import StatusBadge from '../components/StatusBadge'

export default function ReleasesPage() {
  const [releases, setReleases] = useState<any[]>([])
  const [projects, setProjects] = useState<any[]>([])
  const [builds, setBuilds] = useState<any[]>([])
  const [projectId, setProjectId] = useState('')
  const [buildId, setBuildId] = useState('')
  const [version, setVersion] = useState('1.0.0')
  const [channel, setChannel] = useState('production')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  const load = async () => {
    const [r, p, b] = await Promise.all([releasesApi.list(), projectsApi.list(), buildsApi.list()])
    setReleases(Array.isArray(r.data) ? r.data : [])
    setProjects(Array.isArray(p.data) ? p.data : [])
    setBuilds(Array.isArray(b.data) ? b.data : [])
  }

  useEffect(() => { load().catch(() => setError('Failed to load releases')) }, [])

  const create = async (e: React.FormEvent) => {
    e.preventDefault()
    const project = projects.find((x) => x.id === projectId)
    if (!project || !buildId) {
      setError('Select a project and a build')
      return
    }
    setSaving(true)
    setError('')
    try {
      await releasesApi.create({
        buildId,
        applicationId: project.applicationId,
        version,
        channel,
        mandatory: false,
        releaseNotes: notes,
      })
      await load()
    } catch (err: any) {
      setError(err?.response?.data?.error || 'Failed to create release')
    } finally {
      setSaving(false)
    }
  }

  const publish = async (id: string) => {
    try {
      await releasesApi.publish(id, { channel, mandatory: false, releaseNotes: notes })
      await load()
    } catch (err: any) {
      setError(err?.response?.data?.error || 'Publish failed')
    }
  }

  const rollback = async (id: string) => {
    try {
      await releasesApi.rollback(id, 'Rolled back from dashboard')
      await load()
    } catch (err: any) {
      setError(err?.response?.data?.error || 'Rollback failed')
    }
  }

  const projectBuilds = builds.filter((b) => !projectId || b.projectId === projectId)

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-bold text-white">Releases</h1>
      {error && <div className="bg-red-900/40 border border-red-700 text-red-300 rounded-lg p-3 text-sm">{error}</div>}

      <form onSubmit={create} className="bg-gray-900 border border-gray-800 rounded-xl p-5 grid grid-cols-2 gap-4">
        <h2 className="col-span-2 text-white font-semibold">Create release</h2>
        <select value={projectId} onChange={(e) => setProjectId(e.target.value)} className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white">
          <option value="">Select project</option>
          {projects.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
        </select>
        <select value={buildId} onChange={(e) => setBuildId(e.target.value)} className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white">
          <option value="">Select build</option>
          {projectBuilds.map((b) => <option key={b.id} value={b.id}>#{b.buildNumber} · {b.status}</option>)}
        </select>
        <input value={version} onChange={(e) => setVersion(e.target.value)} placeholder="Version" className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white" />
        <select value={channel} onChange={(e) => setChannel(e.target.value)} className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white">
          {['development', 'alpha', 'beta', 'production'].map((c) => <option key={c} value={c}>{c}</option>)}
        </select>
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Release notes" className="col-span-2 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white min-h-20" />
        <button disabled={saving} className="bg-blue-600 hover:bg-blue-700 text-white rounded-lg px-4 py-2 text-sm w-fit">
          {saving ? 'Creating...' : 'Create draft'}
        </button>
      </form>

      <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="border-b border-gray-800 text-left text-xs uppercase text-gray-400">
              <th className="px-4 py-3">Version</th>
              <th className="px-4 py-3">Channel</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Notes</th>
              <th className="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody>
            {releases.map((r) => (
              <tr key={r.id} className="border-b border-gray-800">
                <td className="px-4 py-3 text-white">v{r.version} (#{r.buildNumber})</td>
                <td className="px-4 py-3 text-gray-400">{r.channel}</td>
                <td className="px-4 py-3"><StatusBadge status={r.status} /></td>
                <td className="px-4 py-3 text-gray-500 text-sm">{r.releaseNotes || '—'}</td>
                <td className="px-4 py-3 text-right space-x-2">
                  {r.status === 'Draft' && <button onClick={() => publish(r.id)} className="text-xs bg-green-700 px-3 py-1 rounded">Publish</button>}
                  {r.status === 'Published' && <button onClick={() => rollback(r.id)} className="text-xs bg-red-800 px-3 py-1 rounded">Rollback</button>}
                </td>
              </tr>
            ))}
            {releases.length === 0 && (
              <tr><td colSpan={5} className="px-4 py-10 text-center text-gray-500">No releases yet</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
