// ✅ แค่บอก TypeScript ว่า self ในไฟล์นี้คืออะไร
// export {} บังคับให้ TypeScript มองไฟล์นี้เป็น Module (ต้องมี)
/// <reference lib="webworker" />
export {};
declare const self: SharedWorkerGlobalScope;

// ─── Types ──────────────────────────────────────────────────────
type TabMessage =
  | { type: 'INIT_CONFIG'; apiUrl: string }
  | { type: 'LOGOUT' }
  | { type: 'REQUEST_TOKEN_REFRESH' }
  | { type: 'SET_USER'; user: any }
  | { type: 'CHECK_INITIAL_HEALTH' };

type WorkerMessage =
  | { type: 'STATE_UPDATE'; payload: SharedState }
  | { type: 'FORCE_LOGOUT' }
  | { type: 'TOKEN_REFRESHED'; token: string }
  | { type: 'HEALTH_UPDATE'; reachable: boolean };

interface SharedState {
  user: any | null;
  lastUpdatedAt: number;
}

// ─── Worker State ────────────────────────────────────────────────
let apiUrl = ''; // รอรับจาก Main Thread
let sharedState: SharedState = { user: null, lastUpdatedAt: 0 };
let isRefreshing = false;
let isBackendReachable = true;
let pollTimer: any = null;

// ✅ ใช้ Set เดียวเพื่อจัดการ Port ทั้งหมด (ป้องกันการรั่วไหลของข้อมูล)
const connectedPorts = new Set<MessagePort>();

const ONLINE_MS = 10_000; // ✅ ปรับลดเหลือ 10 วินาที (สมดุลระหว่าง UX กับ Server Load)
const OFFLINE_MS = 5_000; // เช็คทุก 5 วินาทีเมื่อตรวจพบว่าหลุด
const FETCH_TIMEOUT = 3_000; // ✅ ลด Timeout เหลือ 3 วินาที เพื่อให้ Catch ทำงานไวขึ้น

// ─── Health Check Logic (Cost Optimization) ──────────────────────
async function checkHealth() {
  if (!apiUrl) return;

  // ✅ 2. ใช้ AbortController เพื่อตัด Request ที่ค้างนานเกินไป
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), FETCH_TIMEOUT);

  try {
    // ✅ ใช้ HEAD + no-store เพื่อลด Payload และ Bandwidth ให้เหลือศูนย์
    const res = await fetch(`${apiUrl}health`, {
      method: 'HEAD',
      cache: 'no-store',
      signal: controller.signal, // ✅ เชื่อมต่อ signal
    });

    const currentStatus = res.ok;

    if (currentStatus !== isBackendReachable) {
      isBackendReachable = currentStatus;
      broadcastAll({ type: 'HEALTH_UPDATE', reachable: isBackendReachable });
    }
  } catch (err) {
    // ✅ เข้า catch เมื่อเน็ตหลุด, Server ปิด, หรือโดน Abort (Timeout)
    if (isBackendReachable) {
      isBackendReachable = false;
      broadcastAll({ type: 'HEALTH_UPDATE', reachable: false });
    }
  } finally {
    clearTimeout(timeoutId); // ล้าง timeout เสมอ
    scheduleNextHealthCheck();
  }
}

function scheduleNextHealthCheck() {
  clearTimeout(pollTimer);
  // ✅ 3. ใช้ค่า Delay ที่ปรับปรุงใหม่
  const delay = isBackendReachable ? ONLINE_MS : OFFLINE_MS;
  pollTimer = setTimeout(checkHealth, delay);
}

// ─── Messaging ──────────────────────────────────────────────────
function broadcastAll(message: WorkerMessage, excludePort?: MessagePort) {
  connectedPorts.forEach((port) => {
    if (port !== excludePort) {
      try {
        port.postMessage(message);
      } catch {
        connectedPorts.delete(port); // ล้าง Port ที่ตายแล้ว
      }
    }
  });
}

self.onconnect = (e: MessageEvent) => {
  const port = e.ports[0];
  connectedPorts.add(port);

  port.onmessage = async (event: MessageEvent<TabMessage>) => {
    const data = event.data;

    switch (data.type) {
      case 'INIT_CONFIG':
        apiUrl = data.apiUrl;
        if (!pollTimer) checkHealth(); // เริ่มเช็คสุขภาพเมื่อได้ URL
        break;

      case 'SET_USER':
        sharedState = { user: data.user, lastUpdatedAt: Date.now() };
        broadcastAll({ type: 'STATE_UPDATE', payload: sharedState }, port);
        break;

      case 'LOGOUT':
        sharedState = { user: null, lastUpdatedAt: Date.now() };
        broadcastAll({ type: 'FORCE_LOGOUT' });
        break;

      case 'REQUEST_TOKEN_REFRESH':
        if (isRefreshing) break;
        isRefreshing = true;
        try {
          const res = await fetch(`${apiUrl}account/refresh`, { method: 'POST' });
          if (res.ok) {
            const user = await res.json();
            sharedState = { user, lastUpdatedAt: Date.now() };
            broadcastAll({ type: 'TOKEN_REFRESHED', token: user.token });
          } else {
            broadcastAll({ type: 'FORCE_LOGOUT' });
          }
        } catch {
          // ถ้าต่อเน็ตไม่ได้ตอน refresh ไม่ต้อง logout ทันที ให้รอ retry
        } finally {
          isRefreshing = false;
        }
        break;

      case 'CHECK_INITIAL_HEALTH':
        port.postMessage({ type: 'HEALTH_UPDATE', reachable: isBackendReachable });
        checkHealth();
        break;
    }
  };

  port.start();
  port.postMessage({ type: 'STATE_UPDATE', payload: sharedState });
};
