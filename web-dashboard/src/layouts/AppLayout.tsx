import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

const navItems = [
  { path: '/dashboard', icon: '🏠', label: 'Dashboard' },
  { path: '/projects', icon: '📦', label: 'Projects' },
  { path: '/builds', icon: '🔨', label: 'Builds' },
  { path: '/releases', icon: '🚀', label: 'Releases' },
  { path: '/devices', icon: '📱', label: 'Devices' },
  { path: '/settings', icon: '⚙️', label: 'Settings' },
]

export default function AppLayout() {
  const location = useLocation()
  const navigate = useNavigate()
  const logout = useAuthStore((s) => s.logout)

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <div className="flex h-screen bg-gray-950 text-white overflow-hidden">
      {/* Sidebar */}
      <aside className="w-56 bg-gray-900 border-r border-gray-800 flex flex-col shrink-0">
        <div className="h-14 flex items-center px-4 border-b border-gray-800">
          <span className="font-bold text-blue-400 text-lg">Flutter Platform</span>
        </div>
        <nav className="flex-1 py-4 px-2 space-y-0.5">
          {navItems.map(({ path, icon, label }) => (
            <Link
              key={path}
              to={path}
              className={`flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                location.pathname === path || location.pathname.startsWith(path + '/')
                  ? 'bg-blue-600/20 text-blue-400'
                  : 'text-gray-400 hover:text-white hover:bg-gray-800'
              }`}
            >
              <span>{icon}</span>
              <span>{label}</span>
            </Link>
          ))}
        </nav>
        <div className="p-3 border-t border-gray-800">
          <button
            onClick={handleLogout}
            className="w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-gray-400 hover:text-white hover:bg-gray-800 transition-colors"
          >
            <span>🚪</span>
            <span>Sign out</span>
          </button>
        </div>
      </aside>

      {/* Main */}
      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
    </div>
  )
}
