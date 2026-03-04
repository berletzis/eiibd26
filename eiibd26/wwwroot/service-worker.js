// Service Worker para PWA + Push Notifications
const CACHE_NAME = 'eiibd-v6'; // Updated - Remove texto from mood notification
const urlsToCache = [
  '/',
  '/offline.html',
  '/img/avatar-placeholder.png'
];

// Install event
self.addEventListener('install', event => {
  console.log('[SW] Installing...');
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('[SW] Caching offline page');
        return cache.addAll(urlsToCache);
      })
      .then(() => {
        console.log('[SW] Skip waiting');
        return self.skipWaiting();
      })
      .catch(err => console.error('[SW] Install failed:', err))
  );
});

// Activate event
self.addEventListener('activate', event => {
  console.log('[SW] Activating...');
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cacheName => {
          if (cacheName !== CACHE_NAME) {
            console.log('[SW] Deleting old cache:', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    }).then(() => {
      console.log('[SW] Claiming clients');
      return self.clients.claim();
    })
  );
});

// Fetch event (Network First, cache only own resources)
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);

  // Skip non-GET requests
  if (event.request.method !== 'GET') return;

  // Skip chrome-extension and other non-http(s) schemes
  if (!url.protocol.startsWith('http')) return;

  // Skip external resources (CDNs, Google APIs, etc.)
  if (url.origin !== location.origin) {
    return;
  }

  // ⭐ CRITICAL: Skip ALL API endpoints - they should NEVER be cached
  if (url.pathname.startsWith('/api/')) {
    console.log('[SW] Skipping API endpoint:', url.pathname);
    return; // Let the request go through normally
  }

  // Only handle same-origin requests
  event.respondWith(
    fetch(event.request)
      .then(response => {
        // Only cache successful responses from our origin
        if (response && response.status === 200 && response.type === 'basic') {
          const responseToCache = response.clone();
          caches.open(CACHE_NAME)
            .then(cache => cache.put(event.request, responseToCache))
            .catch(err => console.warn('[SW] Cache put failed:', err));
        }
        return response;
      })
      .catch(err => {
        console.log('[SW] Fetch failed, trying cache:', url.pathname);
        return caches.match(event.request)
          .then(response => {
            if (response) {
              return response;
            }
            // If it's a navigation request, return offline page
            if (event.request.mode === 'navigate') {
              return caches.match('/offline.html');
            }
            // For other failed requests, reject
            return Promise.reject(err);
          });
      })
  );
});

// Push Notification event
self.addEventListener('push', event => {
  console.log('[SW] Push event received');

  const data = event.data ? event.data.json() : {};
  const title = data.title || 'EIIBD';
  const options = {
    body: data.body || 'Nueva notificación',
    icon: data.icon || '/img/icons/icon-192x192.png',
    badge: '/img/icons/icon-72x72.png',
    vibrate: [200, 100, 200],
    data: {
      url: data.url || '/',
      timestamp: Date.now(),
      actionData: data.actionData || null // ← Metadata para procesar acciones
    },
    requireInteraction: false,
    tag: data.tag || 'eiibd-notification'
  };

  // ⭐ NUEVO: Agregar botones de acción si están definidos
  if (data.actions && Array.isArray(data.actions)) {
    options.actions = data.actions;
    options.requireInteraction = true; // Mantener visible para que usuario vea los botones
    console.log('[SW] Notification with actions:', data.actions);
  }

  event.waitUntil(
    self.registration.showNotification(title, options)
  );
});

// Notification click event
self.addEventListener('notificationclick', event => {
  console.log('[SW] Notification click. Action:', event.action);

  event.notification.close();

  // ⭐ NUEVO: Si hizo clic en un botón de acción
  if (event.action) {
    event.waitUntil(handleNotificationAction(event));
    return;
  }

  // Click en la notificación misma (no en botón)
  const urlToOpen = event.notification.data.url || '/';

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true })
      .then(windowClients => {
        for (let client of windowClients) {
          if (client.url === urlToOpen && 'focus' in client) {
            return client.focus();
          }
        }
        if (clients.openWindow) {
          return clients.openWindow(urlToOpen);
        }
      })
  );
});

// ⭐ NUEVO: Manejar acciones de notificación
async function handleNotificationAction(event) {
  const action = event.action;
  const actionData = event.notification.data?.actionData;

  console.log('[SW] Handle action:', action, 'Data:', actionData);

  try {
    // === ESTADO DE ÁNIMO ===
    if (action.startsWith('mood-')) {
      const mood = action.replace('mood-', ''); // "bien", "normal", "mal"

      // Mapear a valores del enum (1-5)
      const moodMap = {
        'muy-mal': 1,
        'mal': 2,
        'normal': 3,
        'bien': 4,
        'muy-bien': 5
      };

      const moodValue = moodMap[mood] || 3; // Default: normal

      // Usar FormData en lugar de JSON (el controlador existente usa [FromForm])
      const formData = new FormData();
      formData.append('mood', moodValue.toString());
      // No agregar texto - dejar vacío cuando viene de notificación

      const response = await fetch('/api/EstadoAnimoUsuario/nuevo', {
        method: 'POST',
        body: formData,
        credentials: 'include'
      });

      if (response.ok) {
        await self.registration.showNotification('✅ ¡Guardado!', {
          body: `Tu estado de ánimo ha sido registrado`,
          icon: '/img/icons/icon-192x192.png',
          tag: 'mood-confirmation',
          requireInteraction: false
        });
        console.log('[SW] Mood saved:', mood);
      } else {
        throw new Error(`HTTP ${response.status}`);
      }
    }

    // === ABRIR PÁGINA ===
    else if (action === 'view' || action === 'open') {
      const url = actionData?.url || event.notification.data.url || '/';
      await clients.openWindow(url);
    }

    // === DESCARTAR ===
    else if (action === 'dismiss' || action === 'close') {
      console.log('[SW] User dismissed notification');
    }

    // === RESPUESTA RÁPIDA (para futuras encuestas) ===
    else if (action.startsWith('survey-')) {
      const answer = action.replace('survey-', '');

      await fetch('/api/surveys/respond', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          surveyId: actionData?.surveyId,
          answer: answer,
          timestamp: new Date().toISOString()
        }),
        credentials: 'include'
      });

      await self.registration.showNotification('✅ Respuesta enviada', {
        body: 'Gracias por tu participación',
        icon: '/img/icons/icon-192x192.png',
        tag: 'survey-confirmation'
      });
    }

  } catch (error) {
    console.error('[SW] Error handling action:', error);

    await self.registration.showNotification('❌ Error', {
      body: 'No se pudo procesar. Intenta desde la app.',
      icon: '/img/icons/icon-192x192.png',
      tag: 'error-notification'
    });
  }
}
