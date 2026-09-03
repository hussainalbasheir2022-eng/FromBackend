import { useAuthStore } from '../store/authStore'
import { useNavigate } from 'react-router-dom'

export default function SettingsPage() {
  const logout = useAuthStore((s) => s.logout)
  const navigate = useNavigate()

  return (
    <div className="p-6 max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold text-white">Settings</h1>
      <div className="bg-gray-900 border border-gray-800 rounded-xl p-5 space-y-3 text-sm">
        <p className="text-gray-400">API</p>
        <p className="text-white">http://localhost:5194/api/v1</p>
        <p className="text-gray-400 pt-2">Local mode</p>
        <p className="text-gray-300">SQL Server LocalDB + in-memory build queue. Flutter APK compilation needs the Docker build worker.</p>
        <button
          onClick={() => { logout(); navigate('/login') }}
          className="mt-4 bg-gray-800 hover:bg-gray-700 text-white px-4 py-2 rounded-lg"
        >
          Sign out
        </button>
      </div>
    </div>
  )
}
