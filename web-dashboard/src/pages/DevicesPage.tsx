import { useEffect, useState } from 'react'
import { devicesApi } from '../api/client'
import { deploymentHub } from '../signalr/hub'

export default function DevicesPage() {
  const [devices, setDevices] = useState<any[]>([])

  const load = () => devicesApi.list().then((r) => setDevices(r.data))

  useEffect(() => {
    load()

    deploymentHub.on('device.online', (data: any) => {
      setDevices((prev) =>
        prev.map((d) => d.deviceIdentifier === data.deviceId
          ? { ...d, status: 'Online', appVersion: data.appVersion }
          : d
        )
      )
    })
    deploymentHub.on('device.offline', (data: any) => {
      setDevices((prev) =>
        prev.map((d) => d.deviceIdentifier === data.deviceId ? { ...d, status: 'Offline' } : d)
      )
    })
    deploymentHub.on('device.versionChanged', (data: any) => {
      setDevices((prev) =>
        prev.map((d) => d.deviceIdentifier === data.deviceId ? { ...d, appVersion: data.newVersion } : d)
      )
    })

    return () => {
      deploymentHub.off('device.online')
      deploymentHub.off('device.offline')
      deploymentHub.off('device.versionChanged')
    }
  }, [])

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-white">Devices</h1>
        <button onClick={load} className="text-sm text-gray-400 hover:text-white">↻ Refresh</button>
      </div>

      <div className="bg-gray-900 rounded-xl border border-gray-800 overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="border-b border-gray-800 text-left">
              {['Device', 'App Version', 'OS', 'Channel', 'Battery', 'Status', 'Last Seen'].map((h) => (
                <th key={h} className="px-4 py-3 text-xs font-semibold text-gray-400 uppercase tracking-wider">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {devices.map((d) => (
              <tr key={d.id} className="border-b border-gray-800 hover:bg-gray-800/50">
                <td className="px-4 py-3">
                  <div className="font-medium text-white text-sm">{d.deviceName}</div>
                  <div className="text-xs text-gray-500">{d.deviceIdentifier}</div>
                </td>
                <td className="px-4 py-3 text-sm text-white">v{d.appVersion}</td>
                <td className="px-4 py-3 text-sm text-gray-400">{d.osVersion}</td>
                <td className="px-4 py-3">
                  <span className="text-xs bg-gray-800 border border-gray-700 rounded px-2 py-0.5 text-gray-300">
                    {d.updateChannel}
                  </span>
                </td>
                <td className="px-4 py-3 text-sm text-gray-400">
                  {d.batteryLevel != null ? `${d.batteryLevel}%` : '—'}
                </td>
                <td className="px-4 py-3">
                  <span className={`inline-flex items-center gap-1.5 text-xs px-2 py-0.5 rounded-full ${
                    d.status === 'Online' ? 'bg-green-900/40 text-green-400' : 'bg-gray-800 text-gray-500'
                  }`}>
                    <span className={`w-1.5 h-1.5 rounded-full ${d.status === 'Online' ? 'bg-green-400' : 'bg-gray-600'}`} />
                    {d.status}
                  </span>
                </td>
                <td className="px-4 py-3 text-xs text-gray-500">
                  {new Date(d.lastSeenAt).toLocaleString()}
                </td>
              </tr>
            ))}
            {devices.length === 0 && (
              <tr>
                <td colSpan={7} className="px-4 py-12 text-center text-gray-500">
                  No devices registered yet
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
