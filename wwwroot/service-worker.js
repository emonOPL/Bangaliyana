// Bangaliyana Service Worker
// Version 1.0.0

const CACHE_NAME = 'bangaliyana-cache-v1';
const OFFLINE_URL = '/offline.html';

// Resources to cache immediately on install
const PRECACHE_RESOURCES = [
    '/',
    '/offline.html',
    '/css/site.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/jquery/dist/jquery.min.js',
    '/images/noimage.jpg',
    '/manifest.json'
];

// Install event - precache essential resources
self.addEventListener('install', event => {
    console.log('[ServiceWorker] Installing...');
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[ServiceWorker] Precaching resources');
                return cache.addAll(PRECACHE_RESOURCES);
            })
            .then(() => {
                console.log('[ServiceWorker] Installation complete');
                return self.skipWaiting();
            })
            .catch(err => {
                console.error('[ServiceWorker] Precache failed:', err);
            })
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', event => {
    console.log('[ServiceWorker] Activating...');
    event.waitUntil(
        caches.keys()
            .then(cacheNames => {
                return Promise.all(
                    cacheNames.map(cacheName => {
                        if (cacheName !== CACHE_NAME) {
                            console.log('[ServiceWorker] Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        }
                    })
                );
            })
            .then(() => {
                console.log('[ServiceWorker] Activation complete');
                return self.clients.claim();
            })
    );
});

// Fetch event - implement caching strategies
self.addEventListener('fetch', event => {
    const request = event.request;
    const url = new URL(request.url);

    // Skip non-GET requests
    if (request.method !== 'GET') {
        return;
    }

    // Skip SignalR and WebSocket connections
    if (url.pathname.includes('/notificationHub') ||
        url.pathname.includes('/supportChatHub') ||
        url.pathname.includes('/hangfire')) {
        return;
    }

    // Skip cross-origin requests (CDNs, external APIs)
    if (url.origin !== location.origin) {
        return;
    }

    // API requests - Network First strategy
    if (url.pathname.startsWith('/api/') ||
        url.pathname.includes('/Customer/') ||
        url.pathname.includes('/Admin/') ||
        url.pathname.includes('/Seller/') ||
        url.pathname.includes('/Identity/')) {
        event.respondWith(networkFirst(request));
        return;
    }

    // Static assets - Cache First strategy
    if (isStaticAsset(url.pathname)) {
        event.respondWith(cacheFirst(request));
        return;
    }

    // All other requests - Network First with offline fallback
    event.respondWith(networkFirstWithOfflineFallback(request));
});

// Check if request is for a static asset
function isStaticAsset(pathname) {
    const staticExtensions = [
        '.css', '.js', '.png', '.jpg', '.jpeg', '.gif', '.svg',
        '.ico', '.woff', '.woff2', '.ttf', '.eot'
    ];
    return staticExtensions.some(ext => pathname.endsWith(ext));
}

// Cache First strategy - good for static assets
async function cacheFirst(request) {
    const cachedResponse = await caches.match(request);
    if (cachedResponse) {
        // Update cache in background
        updateCache(request);
        return cachedResponse;
    }

    try {
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }
        return networkResponse;
    } catch (error) {
        console.error('[ServiceWorker] Cache First failed:', error);
        return new Response('Asset not available offline', { status: 503 });
    }
}

// Network First strategy - good for dynamic content
async function networkFirst(request) {
    try {
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }
        return networkResponse;
    } catch (error) {
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }
        throw error;
    }
}

// Network First with offline fallback
async function networkFirstWithOfflineFallback(request) {
    try {
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }
        return networkResponse;
    } catch (error) {
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }

        // Return offline page for navigation requests
        if (request.mode === 'navigate') {
            return caches.match(OFFLINE_URL);
        }

        throw error;
    }
}

// Update cache in background
async function updateCache(request) {
    try {
        const networkResponse = await fetch(request);
        if (networkResponse.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }
    } catch (error) {
        // Silently fail for background updates
    }
}

// Push notification event
self.addEventListener('push', event => {
    console.log('[ServiceWorker] Push received');

    let data = { title: 'Bangaliyana', body: 'You have a new notification!' };

    if (event.data) {
        try {
            data = event.data.json();
        } catch (e) {
            data.body = event.data.text();
        }
    }

    const options = {
        body: data.body || data.message,
        icon: '/icons/icon-192x192.png',
        badge: '/icons/icon-72x72.png',
        vibrate: [100, 50, 100],
        data: {
            url: data.url || '/',
            dateOfArrival: Date.now()
        },
        actions: [
            { action: 'open', title: 'Open' },
            { action: 'close', title: 'Close' }
        ]
    };

    event.waitUntil(
        self.registration.showNotification(data.title, options)
    );
});

// Notification click event
self.addEventListener('notificationclick', event => {
    console.log('[ServiceWorker] Notification clicked');

    event.notification.close();

    if (event.action === 'close') {
        return;
    }

    const urlToOpen = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(windowClients => {
                // Check if there's already a window open
                for (let client of windowClients) {
                    if (client.url === urlToOpen && 'focus' in client) {
                        return client.focus();
                    }
                }
                // Open new window
                if (clients.openWindow) {
                    return clients.openWindow(urlToOpen);
                }
            })
    );
});

// Background sync event (for offline actions)
self.addEventListener('sync', event => {
    console.log('[ServiceWorker] Sync event:', event.tag);

    if (event.tag === 'sync-cart') {
        event.waitUntil(syncCart());
    }
});

// Sync cart data when back online
async function syncCart() {
    try {
        // Retrieve pending cart actions from IndexedDB
        // and sync them with the server
        console.log('[ServiceWorker] Syncing cart...');
    } catch (error) {
        console.error('[ServiceWorker] Cart sync failed:', error);
    }
}

// Message event - for communication with the main app
self.addEventListener('message', event => {
    console.log('[ServiceWorker] Message received:', event.data);

    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }

    if (event.data && event.data.type === 'CLEAR_CACHE') {
        caches.delete(CACHE_NAME);
    }
});
