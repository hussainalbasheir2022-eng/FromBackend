import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { projectsApi, buildsApi, devicesApi } from '../api/client'
import StatusBadge from '../components/StatusBadge'

export default function DashboardPage() {
  const [projects, setProjects] = useState<any[]>([])
  const [builds, setBuilds] = useState<any[]>([])
  const [devices, setDevices] = useState<any[]>([])

  useEffect(() => {
    projectsApi.list().then((r) => setProjects(r.data))
    buildsApi.list().then((r) => setBuilds(r.data.slice(0, 5)))
    devicesApi.list().then((r) => setDevices(r.data))
  }, [])

  const onlineDevices = devices.filter((d) => d.status === 'Online').length
  const recentBuilds = builds.slice(0, 5)

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-bold text-white">Dashboard</h1>

      {/* Metrics */}
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Projects', value: projects.length, color: 'blue' },
          { label: 'Total Devices', value: devices.length, color: 'purple' },
          { label: 'Online Devices', value: onlineDevices, color: 'green' },
          { label: 'Recent Builds', value: builds.length, color: 'yellow' },
        ].map(({ label, value, color }) => (
          <div key={label} className="bg-gray-900 rounded-xl border border-gray-800 p-4">
            <p className="text-gray-400 text-sm">{label}</p>
            <p className={`text-3xl font-bold mt-1 text-${color}-400`}>{value}</p>
          </div>
        ))}
      </div>

      {/* Projects */}
      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-white">Projects</h2>
          <Link to="/projects/new" className="text-xs bg-blue-600 hover:bg-blue-700 text-white px-3 py-1.5 rounded">
            New Project
          </Link>
        </div>
        <div className="space-y-2">
          {projects.map((p) => (
            <div key={p.id} className="flex items-center justify-between bg-gray-800 rounded-lg p-3">
              <div>
                <p className="font-medium text-white">{p.name}</p>
                <p className="text-xs text-gray-400">{p.applicationId} · v{p.version}</p>
              </div>
              <div className="flex gap-2">
                <Link
                  to={`/ide/${p.id}`}
                  className="text-xs bg-gray-700 hover:bg-gray-600 text-white px-3 py-1 rounded"
                >
                  Open IDE
                </Link>
              </div>
            </div>
          ))}
          {projects.length === 0 && (
            <p className="text-gray-500 text-sm text-center py-4">
              No projects yet. <Link to="/projects/new" className="text-blue-400 hover:underline">Create one</Link>
            </p>
          )}
        </div>
      </div>

      {/* Recent Builds */}
      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <h2 className="text-lg font-semibold text-white mb-4">Recent Builds</h2>
        <div className="space-y-2">
          {recentBuilds.map((b) => (
            <div key={b.id} className="flex items-center justify-between bg-gray-800 rounded-lg p-3">
              <div>
                <p className="font-medium text-white">Build #{b.buildNumber}</p>
                <p className="text-xs text-gray-400">{new Date(b.createdAt).toLocaleString()}</p>
              </div>
              <StatusBadge status={b.status} />
            </div>
          ))}
          {recentBuilds.length === 0 && (
            <p className="text-gray-500 text-sm text-center py-4">No builds yet</p>
          )}
        </div>
      </div>

      {/* Devices */}
      <div className="bg-gray-900 rounded-xl border border-gray-800 p-5">
        <h2 className="text-lg font-semibold text-white mb-4">Devices</h2>
        <div className="grid grid-cols-3 gap-3">
          {devices.map((d) => (
            <div key={d.id} className="bg-gray-800 rounded-lg p-3">
              <div className="flex items-center justify-between mb-1">
                <p className="font-medium text-white text-sm">{d.deviceName}</p>
                <span className={`w-2 h-2 rounded-full ${d.status === 'Online' ? 'bg-green-400' : 'bg-gray-600'}`} />
              </div>
              <p className="text-xs text-gray-400">v{d.appVersion}</p>
              <p className="text-xs text-gray-500">{d.updateChannel}</p>
            </div>
          ))}
          {devices.length === 0 && (
            <p className="col-span-3 text-gray-500 text-sm text-center py-4">No devices registered</p>
          )}
        </div>
      </div>
    </div>
  )
}

