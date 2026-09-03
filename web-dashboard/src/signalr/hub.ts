import * as signalR from '@microsoft/signalr'

const BASE = import.meta.env.VITE_SIGNALR_URL || ''

function createHub(path: string) {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${BASE}${path}`, {
      accessTokenFactory: () => localStorage.getItem('accessToken') || '',
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build()
}

export const buildHub = createHub('/hubs/build')
export const deploymentHub = createHub('/hubs/deployment')

export async function startHubs() {
  try {
    await buildHub.start()
    console.log('Build hub connected')
  } catch (e) {
    console.warn('Build hub failed to connect', e)
  }

  try {
    await deploymentHub.start()
    console.log('Deployment hub connected')
  } catch (e) {
    console.warn('Deployment hub failed to connect', e)
  }
}
