/**
 * Service Worker for Push Notifications
 * Handles background push events and notification display
 */

self.addEventListener('install', event => {
    console.log('Notification Service Worker installed');
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    console.log('Notification Service Worker activated');
    event.waitUntil(clients.claim());
});

self.addEventListener('push', event => {
    if (!event.data) return;

    let data;
    try {
        data = event.data.json();
    } catch (e) {
        data = {
            title: 'New Notification',
            message: event.data.text(),
            type: 'general'
        };
    }

    const options = {
        body: data.message || data.body,
        icon: '/images/logo-192.png',
        badge: '/images/badge-72.png',
        vibrate: [100, 50, 100],
        data: {
            url: data.link || data.url || '/',
            notificationId: data.id
        },
        actions: [
            { action: 'view', title: 'View' },
            { action: 'dismiss', title: 'Dismiss' }
        ],
        requireInteraction: data.type === 'order',
        tag: data.id ? `notification-${data.id}` : undefined,
        renotify: true
    };

    event.waitUntil(
        self.registration.showNotification(data.title, options)
    );
});

self.addEventListener('notificationclick', event => {
    event.notification.close();

    if (event.action === 'dismiss') {
        return;
    }

    const urlToOpen = event.notification.data?.url || '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(clientList => {
                // Focus existing window if available
                for (const client of clientList) {
                    if (client.url.includes(self.location.origin) && 'focus' in client) {
                        client.focus();
                        client.navigate(urlToOpen);
                        return;
                    }
                }
                // Open new window
                return clients.openWindow(urlToOpen);
            })
    );
});

self.addEventListener('notificationclose', event => {
    // Optional: Track dismissed notifications
    console.log('Notification dismissed:', event.notification.data?.notificationId);
});
