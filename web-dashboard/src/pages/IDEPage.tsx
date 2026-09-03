import { useEffect, useState, useCallback } from 'react'
import { useParams } from 'react-router-dom'
import Editor from '@monaco-editor/react'
import { filesApi, projectsApi, buildsApi } from '../api/client'
import { buildHub } from '../signalr/hub'

interface FileNode {
  id: string
  path: string
  name: string
  size: number
}

interface BuildLog {
  message: string
  level: string
  timestamp: string
}

export default function IDEPage() {
  const { projectId } = useParams<{ projectId: string }>()
  const [project, setProject] = useState<any>(null)
  const [files, setFiles] = useState<FileNode[]>([])
  const [selectedPath, setSelectedPath] = useState<string | null>(null)
  const [content, setContent] = useState('')
  const [isDirty, setIsDirty] = useState(false)
  const [buildId, setBuildId] = useState<string | null>(null)
  const [buildStatus, setBuildStatus] = useState<string>('')
  const [buildLogs, setBuildLogs] = useState<BuildLog[]>([])
  const [activeTab, setActiveTab] = useState<'logs' | 'problems'>('logs')
  const [newFileName, setNewFileName] = useState('')
  const [showNewFile, setShowNewFile] = useState(false)
  const [apkBusy, setApkBusy] = useState(false)
  const [apkMsg, setApkMsg] = useState('')

  useEffect(() => {
    if (!projectId) return
    projectsApi.get(projectId).then((r) => setProject(r.data))
    filesApi.list(projectId).then((r) => setFiles(r.data))
  }, [projectId])

  useEffect(() => {
    if (!buildId) return
    if (buildHub.state === 'Disconnected') {
      buildHub.start().catch(console.warn)
    }
    buildHub.invoke('JoinBuildGroup', buildId).catch(console.warn)
    buildHub.on('build.log', (data: any) => {
      if (String(data.buildId) !== String(buildId)) return
      setBuildLogs((prev) => [...prev, { message: data.message, level: data.level, timestamp: data.timestamp }])
    })
    buildHub.on('build.completed', (data: any) => {
      if (String(data.buildId) !== String(buildId)) return
      setBuildStatus(data.success ? 'Succeeded — waiting for devices to update' : 'Failed')
    })
    buildHub.on('build.failed', (data: any) => {
      if (String(data.buildId) !== String(buildId)) return
      setBuildStatus('Failed')
      setBuildLogs((prev) => [...prev, { message: data.error, level: 'error', timestamp: new Date().toISOString() }])
    })
    return () => {
      buildHub.off('build.log')
      buildHub.off('build.completed')
      buildHub.off('build.failed')
    }
  }, [buildId])

  const openFile = async (path: string) => {
    if (!projectId) return
    const res = await filesApi.get(projectId, path)
    setSelectedPath(path)
    setContent(res.data.content || '')
    setIsDirty(false)
  }

  const saveFile = useCallback(async () => {
    if (!projectId || !selectedPath) return
    await filesApi.upsert(projectId, selectedPath, content)
    setIsDirty(false)
  }, [projectId, selectedPath, content])

  // Ctrl+S to save
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault()
        saveFile()
      }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [saveFile])

  const triggerBuild = async () => {
    if (!projectId) return
    await saveFile()
    setBuildLogs([])
    setBuildStatus('Queued')
    setActiveTab('logs')
    const res = await projectsApi.build(projectId)
    setBuildId(res.data.buildId)
  }

  const triggerPublish = async () => {
    if (!projectId) return
    await saveFile()
    setBuildLogs([])
    setBuildStatus('Publishing')
    setActiveTab('logs')
    const res = await projectsApi.publish(projectId, {
      channel: 'production',
      mandatory: true,
      releaseNotes: 'Published from Cloud IDE',
    })
    setBuildId(res.data.buildId)
  }

  const downloadApk = async () => {
    if (!projectId) return
    setApkBusy(true)
    setApkMsg('')
    try {
      await projectsApi.downloadApk(projectId)
      setApkMsg('APK downloaded. Install this file once on the phone.')
    } catch (err: any) {
      setApkMsg(err?.message || 'Download failed')
    } finally {
      setApkBusy(false)
    }
  }

  const createFile = async () => {
    if (!projectId || !newFileName) return
    const path = newFileName.startsWith('lib/') ? newFileName : `lib/${newFileName}`
    await filesApi.upsert(projectId, path, '// New file\n')
    const listRes = await filesApi.list(projectId)
    setFiles(listRes.data)
    setNewFileName('')
    setShowNewFile(false)
    await openFile(path)
  }

  const fileTree = buildFileTree(files)

  return (
    <div className="h-screen flex flex-col bg-gray-950 text-white overflow-hidden">
      {/* ── Top Bar ──────────────────────────────────────────────────── */}
      <div className="h-10 bg-gray-900 border-b border-gray-800 flex items-center px-4 gap-4 shrink-0">
        <span className="text-blue-400 font-semibold text-sm">
          {project?.name ?? 'Loading...'}
        </span>
        <span className="text-gray-600 text-xs">v{project?.version}</span>
        <span className="text-gray-500 text-xs font-mono hidden md:inline" title="APK package bound to this project">
          {project?.applicationId}
        </span>
        <div className="flex-1" />
        {apkMsg && <span className="text-[11px] text-amber-300 max-w-xs truncate" title={apkMsg}>{apkMsg}</span>}
        <button
          onClick={saveFile}
          disabled={!isDirty}
          className="text-xs px-3 py-1 bg-gray-700 hover:bg-gray-600 disabled:opacity-40 rounded"
        >
          Save
        </button>
        <button
          onClick={triggerBuild}
          className="text-xs px-3 py-1 bg-blue-600 hover:bg-blue-700 rounded font-medium"
        >
          Build
        </button>
        <button
          className="text-xs px-3 py-1 bg-green-700 hover:bg-green-600 rounded font-medium"
          onClick={triggerPublish}
        >
          Publish
        </button>
        <button
          className="text-xs px-3 py-1 bg-amber-700 hover:bg-amber-600 rounded font-medium disabled:opacity-50"
          onClick={downloadApk}
          disabled={apkBusy}
          title="Download the first APK for this project and install it once on the phone"
        >
          {apkBusy ? 'Downloading…' : 'Share APK'}
        </button>
      </div>

      {/* ── Main Content ─────────────────────────────────────────────── */}
      <div className="flex flex-1 overflow-hidden">
        {/* File Explorer */}
        <div className="w-56 bg-gray-900 border-r border-gray-800 flex flex-col overflow-hidden shrink-0">
          <div className="flex items-center justify-between px-3 py-2 border-b border-gray-800">
            <span className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Explorer</span>
            <button
              onClick={() => setShowNewFile(!showNewFile)}
              className="text-gray-400 hover:text-white text-lg leading-none"
              title="New File"
            >+</button>
          </div>
          {showNewFile && (
            <div className="px-2 py-1 flex gap-1">
              <input
                value={newFileName}
                onChange={(e) => setNewFileName(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && createFile()}
                placeholder="path/file.dart"
                className="flex-1 text-xs bg-gray-800 border border-gray-600 rounded px-2 py-1 text-white"
                autoFocus
              />
            </div>
          )}
          <div className="flex-1 overflow-y-auto py-1">
            {renderTree(fileTree, openFile, selectedPath)}
          </div>
        </div>

        {/* Editor */}
        <div className="flex-1 flex flex-col overflow-hidden">
          {/* Tab bar */}
          {selectedPath && (
            <div className="h-8 bg-gray-900 border-b border-gray-800 flex items-center px-2 shrink-0">
              <div className="flex items-center gap-1 bg-gray-800 rounded px-2 py-0.5 text-xs text-gray-300">
                <span>{selectedPath.split('/').pop()}</span>
                {isDirty && <span className="text-yellow-400">●</span>}
              </div>
            </div>
          )}
          <div className="flex-1 overflow-hidden">
            {selectedPath ? (
              <Editor
                height="100%"
                language={getLanguage(selectedPath)}
                value={content}
                theme="vs-dark"
                onChange={(val) => { setContent(val || ''); setIsDirty(true) }}
                options={{
                  fontSize: 14,
                  minimap: { enabled: true },
                  lineNumbers: 'on',
                  folding: true,
                  wordWrap: 'on',
                  tabSize: 2,
                  formatOnPaste: true,
                  scrollBeyondLastLine: false,
                }}
              />
            ) : (
              <div className="flex items-center justify-center h-full text-gray-600">
                <div className="text-center">
                  <p className="text-4xl mb-4">📝</p>
                  <p className="text-lg">Select a file to start editing</p>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* ── Bottom Panel ─────────────────────────────────────────────── */}
      <div className="h-48 bg-gray-900 border-t border-gray-800 flex flex-col shrink-0">
        <div className="flex items-center gap-4 border-b border-gray-800 px-3">
          {['logs', 'problems'].map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab as any)}
              className={`text-xs py-1.5 capitalize border-b-2 transition-colors ${
                activeTab === tab
                  ? 'border-blue-500 text-white'
                  : 'border-transparent text-gray-500 hover:text-gray-300'
              }`}
            >
              {tab === 'logs' ? `Build Output ${buildStatus ? `(${buildStatus})` : ''}` : tab}
            </button>
          ))}
          {buildId && (
            <button
              onClick={async () => {
                const res = await buildsApi.getLogs(buildId)
                setBuildLogs(res.data)
              }}
              className="ml-auto text-xs text-gray-500 hover:text-gray-300"
            >
              Refresh
            </button>
          )}
        </div>
        <div className="flex-1 overflow-y-auto px-3 py-2 font-mono text-xs">
          {buildLogs.length === 0 ? (
            <span className="text-gray-600">No build output yet. Click Build to start.</span>
          ) : (
            buildLogs.map((log, i) => (
              <div key={i} className={`${log.level === 'error' ? 'text-red-400' : 'text-gray-300'}`}>
                <span className="text-gray-600 mr-2">{new Date(log.timestamp).toLocaleTimeString()}</span>
                {log.message}
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  )
}

// ─── Helpers ──────────────────────────────────────────────────────────────────
function getLanguage(path: string) {
  if (path.endsWith('.dart')) return 'dart'
  if (path.endsWith('.yaml') || path.endsWith('.yml')) return 'yaml'
  if (path.endsWith('.json')) return 'json'
  if (path.endsWith('.md')) return 'markdown'
  if (path.endsWith('.gradle')) return 'groovy'
  if (path.endsWith('.xml')) return 'xml'
  return 'plaintext'
}

interface TreeNode {
  name: string
  path: string
  isDir: boolean
  children: TreeNode[]
}

function buildFileTree(files: FileNode[]): TreeNode[] {
  const root: TreeNode[] = []
  for (const file of files) {
    const parts = file.path.split('/')
    let current = root
    let currentPath = ''
    for (let i = 0; i < parts.length; i++) {
      currentPath = i === 0 ? parts[0] : `${currentPath}/${parts[i]}`
      const isLast = i === parts.length - 1
      let node = current.find((n) => n.name === parts[i])
      if (!node) {
        node = { name: parts[i], path: currentPath, isDir: !isLast, children: [] }
        current.push(node)
      }
      current = node.children
    }
  }
  return root
}

function renderTree(
  nodes: TreeNode[], onOpen: (path: string) => void, selected: string | null, depth = 0
): React.ReactNode {
  return nodes
    .sort((a, b) => (a.isDir === b.isDir ? a.name.localeCompare(b.name) : a.isDir ? -1 : 1))
    .map((node) => (
      <div key={node.path}>
        <div
          style={{ paddingLeft: `${depth * 12 + 8}px` }}
          onClick={() => !node.isDir && onOpen(node.path)}
          className={`flex items-center gap-1.5 py-0.5 cursor-pointer text-sm rounded mx-1 px-1 ${
            selected === node.path
              ? 'bg-blue-600/30 text-white'
              : 'text-gray-400 hover:text-white hover:bg-gray-800'
          }`}
        >
          <span>{node.isDir ? '📁' : getFileIcon(node.name)}</span>
          <span className="truncate">{node.name}</span>
        </div>
        {node.isDir && renderTree(node.children, onOpen, selected, depth + 1)}
      </div>
    ))
}

function getFileIcon(name: string) {
  if (name.endsWith('.dart')) return '🎯'
  if (name.endsWith('.yaml') || name.endsWith('.yml')) return '⚙️'
  if (name.endsWith('.json')) return '📋'
  if (name.endsWith('.md')) return '📄'
  return '📝'
}
