import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { projectsApi } from '../api/client'

function uniquePackage(name: string) {
  const slug = name.toLowerCase().replace(/[^a-z0-9]+/g, '').slice(0, 12) || 'app'
  const suffix = Math.random().toString(36).slice(2, 8)
  return `com.flutterplatform.${slug}_${suffix}`
}

export default function NewProjectPage() {
  const navigate = useNavigate()
  const [name, setName] = useState('My Flutter App')
  const [applicationId, setApplicationId] = useState(() => uniquePackage('My Flutter App'))
  const [displayName, setDisplayName] = useState('My Flutter App')
  const [version, setVersion] = useState('1.0.0')
  const [description, setDescription] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const hint = useMemo(
    () => 'This package name is the APK identity. Install the first APK once; later Publish updates only devices that have this app.',
    [],
  )

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const { data } = await projectsApi.create({
        name,
        applicationId,
        displayName,
        version,
        description,
      })
      navigate(`/ide/${data.id}`)
    } catch (err: any) {
      const msg = err?.response?.data?.error
        || err?.response?.data?.title
        || err?.message
        || 'Failed to create project'
      setError(typeof msg === 'string' ? msg : 'Failed to create project')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="p-6 max-w-2xl">
      <h1 className="text-2xl font-bold text-white mb-6">New Project</h1>
      <form onSubmit={submit} className="bg-gray-900 border border-gray-800 rounded-xl p-6 space-y-4">
        {error && (
          <div className="bg-red-900/40 border border-red-700 text-red-300 rounded-lg p-3 text-sm">{error}</div>
        )}
        <Field label="Project name" value={name} onChange={setName} required />
        <div>
          <Field label="Application ID (Android package name)" value={applicationId} onChange={setApplicationId} required />
          <p className="text-xs text-gray-500 mt-1">{hint}</p>
        </div>
        <Field label="Display name" value={displayName} onChange={setDisplayName} />
        <Field label="Version" value={version} onChange={setVersion} />
        <div>
          <label className="block text-sm text-gray-400 mb-1">Description</label>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-2.5 text-white min-h-24"
          />
        </div>
        <div className="flex gap-3 pt-2">
          <button
            type="submit"
            disabled={loading}
            className="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white px-4 py-2 rounded-lg text-sm"
          >
            {loading ? 'Creating...' : 'Create project'}
          </button>
          <button type="button" onClick={() => navigate('/projects')} className="text-sm text-gray-400 hover:text-white">
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}

function Field({
  label, value, onChange, required,
}: { label: string; value: string; onChange: (v: string) => void; required?: boolean }) {
  return (
    <div>
      <label className="block text-sm text-gray-400 mb-1">{label}</label>
      <input
        value={value}
        required={required}
        onChange={(e) => onChange(e.target.value)}
        className="w-full bg-gray-800 border border-gray-700 rounded-lg px-4 py-2.5 text-white"
      />
    </div>
  )
}
