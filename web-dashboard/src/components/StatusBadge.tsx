export default function StatusBadge({ status }: { status: string }) {
  const colors: Record<string, string> = {
    Succeeded: 'bg-green-900 text-green-300',
    Published: 'bg-green-900 text-green-300',
    Online: 'bg-green-900 text-green-300',
    Failed: 'bg-red-900 text-red-300',
    RolledBack: 'bg-red-900 text-red-300',
    Running: 'bg-blue-900 text-blue-300',
    Pending: 'bg-gray-800 text-gray-300',
    Draft: 'bg-gray-800 text-gray-300',
    Queued: 'bg-yellow-900 text-yellow-300',
    Cancelled: 'bg-gray-700 text-gray-400',
    Archived: 'bg-gray-700 text-gray-400',
    Offline: 'bg-gray-700 text-gray-400',
  }
  return (
    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${colors[status] ?? 'bg-gray-700 text-gray-300'}`}>
      {status}
    </span>
  )
}
