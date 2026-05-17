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
    // ✅ เปลี่ยนมายิงที่ /live แทน /health เฉยๆ
    // การเช็คแบบนี้ฝั่ง Backend จะประมวลผลด้วย CPU เกือบ 0%
    const res = await fetch(`${apiUrl}health/live`, {
      method: 'HEAD', // หรือเปลี่ยนเป็น 'GET' ถ้าระบบเครือข่ายมีปัญหากับ HEAD
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

// ─── Token Utils ─────────────────────────────────────────────────
// ✅ แก้ Base64URL → Base64 ก่อน decode (JWT ใช้ Base64URL ไม่ใช่ Base64 ปกติ)
function base64UrlDecode(str: string): string {
  const base64 = str.replace(/-/g, '+').replace(/_/g, '/');
  return decodeURIComponent(
    atob(base64)
      .split('')
      .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
      .join(''),
  );
}

// ✅ เพิ่มฟังก์ชันช่วยตรวจสอบ JWT ว่าหมดอายุหรือไม่
function isTokenExpired(token: string): boolean {
  try {
    const payloadBase64 = token.split('.')[1];
    if (!payloadBase64) return false;

    const payload = JSON.parse(base64UrlDecode(payloadBase64));

    if (!payload.exp) return false;
    return Date.now() >= payload.exp * 1000;
  } catch {
    return true;
  }
}

// ─── Session Watcher ─────────────────────────────────────────────
let sessionWatcherInterval: any = null;

async function refreshSessionFromCookie(): Promise<boolean> {
  if (!apiUrl) return false;

  const res = await fetch(`${apiUrl}account/refresh`, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
  });

  if (!res.ok) {
    if (res.status === 401 || res.status === 403) {
      sharedState = { user: null, lastUpdatedAt: Date.now() };
      broadcastAll({ type: 'FORCE_LOGOUT' });
    }
    return false;
  }

  const user = await res.json();

  if (user?.token && !isTokenExpired(user.token)) {
    sharedState = { user, lastUpdatedAt: Date.now() };
    broadcastAll({ type: 'STATE_UPDATE', payload: sharedState });
    broadcastAll({ type: 'TOKEN_REFRESHED', token: user.token });
    return true;
  }

  sharedState = { user: null, lastUpdatedAt: Date.now() };
  broadcastAll({ type: 'FORCE_LOGOUT' });
  return false;
}

function startSessionWatcher() {
  // ✅ ป้องกันการรัน Interval ซ้ำซ้อน
  if (sessionWatcherInterval) return;

  sessionWatcherInterval = setInterval(async () => {
    if (sharedState.user?.token && isTokenExpired(sharedState.user.token)) {
      if (isRefreshing) return;
      isRefreshing = true;
      try {
        await refreshSessionFromCookie();
      } catch {
        // ถ้าต่อเน็ตไม่ได้ตอน refresh ไม่ต้อง logout ทันที ให้รอรอบถัดไป
      } finally {
        isRefreshing = false;
      }
    }
  }, 10_000); // เช็คทุก 10 วินาที
}

// ✅ เรียกใช้แค่ครั้งเดียวตอนที่ Worker ถูกโหลดขึ้นมาในหน่วยความจำ
startSessionWatcher();

// ─── Connection Handler ──────────────────────────────────────────
self.onconnect = (e: MessageEvent) => {
  const port = e.ports[0];
  connectedPorts.add(port);

  // ✅ ลบ port ออกเมื่อ Tab ปิด ป้องกัน port สะสมใน Set
  port.addEventListener('close', () => {
    connectedPorts.delete(port);
  });

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
        try {
          await fetch(`${apiUrl}account/logout`, {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
          });
        } catch {
          // ignore network failures on logout cleanup
        }
        sharedState = { user: null, lastUpdatedAt: Date.now() };
        broadcastAll({ type: 'FORCE_LOGOUT' }, port);
        break;

      case 'REQUEST_TOKEN_REFRESH':
        if (isRefreshing) break;
        isRefreshing = true;
        try {
          await refreshSessionFromCookie();
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
