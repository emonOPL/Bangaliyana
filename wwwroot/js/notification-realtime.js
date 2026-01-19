/**
 * Bangaliyana Real-Time Notification System
 * Uses SignalR for real-time updates with polling fallback
 */
(function () {
    'use strict';

    // Configuration
    const config = {
        hubUrl: '/notificationHub',
        pollingInterval: 30000, // 30 seconds fallback
        reconnectInterval: 5000,
        maxReconnectAttempts: 10
    };

    // State
    let connection = null;
    let reconnectAttempts = 0;
    let isConnected = false;
    let pollingTimer = null;
    let lastPollTime = null;

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function () {
        // Only initialize for authenticated users (check if notification badge exists)
        const notificationBadge = document.querySelector('.icon-badge');
        if (notificationBadge) {
            initializeNotifications();
        }
    });

    function initializeNotifications() {
        // Check for SignalR support
        if (typeof signalR !== 'undefined') {
            initializeSignalR();
        } else {
            console.warn('SignalR not available, falling back to polling');
            startPolling();
        }

        // Request browser notification permission on first interaction
        requestNotificationPermission();
    }

    // ==================== SignalR Connection ====================

    function initializeSignalR() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl(config.hubUrl)
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    if (retryContext.previousRetryCount >= config.maxReconnectAttempts) {
                        return null; // Stop retrying
                    }
                    return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                }
            })
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        // Event handlers
        connection.on('ReceiveNotification', handleNotification);
        connection.on('UpdateUnreadCount', updateBadgeCount);
        connection.on('Pong', handlePong);

        // Connection state events
        connection.onreconnecting(error => {
            console.log('SignalR reconnecting...', error);
            isConnected = false;
            updateConnectionStatus('reconnecting');
        });

        connection.onreconnected(connectionId => {
            console.log('SignalR reconnected with ID:', connectionId);
            isConnected = true;
            reconnectAttempts = 0;
            updateConnectionStatus('connected');
            stopPolling();
        });

        connection.onclose(error => {
            console.log('SignalR connection closed', error);
            isConnected = false;
            updateConnectionStatus('disconnected');

            // Start polling as fallback
            startPolling();
        });

        // Start connection
        startConnection();
    }

    async function startConnection() {
        try {
            await connection.start();
            console.log('SignalR notification hub connected');
            isConnected = true;
            reconnectAttempts = 0;
            updateConnectionStatus('connected');

            // Stop polling if it was running
            stopPolling();

            // Join seller group if applicable
            const sellerIdElement = document.querySelector('[data-seller-id]');
            if (sellerIdElement) {
                const sellerId = parseInt(sellerIdElement.dataset.sellerId);
                if (sellerId) {
                    await connection.invoke('JoinSellerGroup', sellerId);
                }
            }

            // Start heartbeat
            startHeartbeat();

        } catch (err) {
            console.error('SignalR connection failed:', err);
            reconnectAttempts++;

            if (reconnectAttempts < config.maxReconnectAttempts) {
                setTimeout(startConnection, config.reconnectInterval);
            } else {
                console.warn('Max reconnect attempts reached, falling back to polling');
                startPolling();
            }
        }
    }

    function startHeartbeat() {
        setInterval(async () => {
            if (isConnected && connection) {
                try {
                    await connection.invoke('Ping');
                } catch (err) {
                    console.warn('Heartbeat failed:', err);
                }
            }
        }, 30000);
    }

    function handlePong(timestamp) {
        // Connection is alive
        console.debug('Pong received:', timestamp);
    }

    // ==================== Notification Handling ====================

    function handleNotification(notification) {
        console.log('Received notification:', notification);

        // Update badge count (increment by 1)
        incrementBadgeCount();

        // Show toast notification
        showToastNotification(notification);

        // Show browser notification if permitted
        showBrowserNotification(notification);

        // Play notification sound (optional)
        playNotificationSound();
    }

    function showToastNotification(notification) {
        // Use alertify which is already available in the project
        if (typeof alertify !== 'undefined') {
            const type = notification.type === 'order' ? 'success' : 'message';
            alertify.notify(
                `<strong>${escapeHtml(notification.title)}</strong><br>${escapeHtml(notification.message)}`,
                type,
                5,
                function () {
                    if (notification.link) {
                        window.location.href = notification.link;
                    }
                }
            );
        } else if (typeof showToast === 'function') {
            showToast(notification.message, 'info');
        }
    }

    function showBrowserNotification(notification) {
        if ('Notification' in window && Notification.permission === 'granted') {
            const browserNotif = new Notification(notification.title, {
                body: notification.message,
                icon: '/favicon.ico',
                tag: `notification-${notification.id}`,
                requireInteraction: false,
                silent: false
            });

            browserNotif.onclick = function () {
                window.focus();
                if (notification.link) {
                    window.location.href = notification.link;
                }
                browserNotif.close();
            };

            // Auto-close after 5 seconds
            setTimeout(() => browserNotif.close(), 5000);
        }
    }

    function playNotificationSound() {
        // Optional: Play a subtle notification sound
        try {
            const audio = new Audio('/sounds/notification.mp3');
            audio.volume = 0.3;
            audio.play().catch(() => { }); // Ignore autoplay restrictions
        } catch (e) {
            // Sound not available
        }
    }

    // ==================== Badge Count Management ====================

    function updateBadgeCount(count) {
        const badges = document.querySelectorAll('.nav-item .icon-badge');
        badges.forEach(badge => {
            if (count > 0) {
                badge.textContent = count > 9 ? '9+' : count.toString();
                badge.classList.remove('d-none');
            } else {
                badge.classList.add('d-none');
            }
        });
    }

    function incrementBadgeCount() {
        const badges = document.querySelectorAll('.nav-item .icon-badge');
        badges.forEach(badge => {
            let current = parseInt(badge.textContent) || 0;
            current++;
            badge.textContent = current > 9 ? '9+' : current.toString();
            badge.classList.remove('d-none');

            // Pulse animation
            badge.style.animation = 'none';
            badge.offsetHeight; // trigger reflow
            badge.style.animation = 'pulse 0.5s ease';
        });
    }

    // ==================== Polling Fallback ====================

    function startPolling() {
        if (pollingTimer) return;

        console.log('Starting notification polling');
        lastPollTime = new Date();

        pollingTimer = setInterval(async () => {
            try {
                const response = await fetch(`/api/notifications/poll?since=${lastPollTime.toISOString()}`);
                if (response.ok) {
                    const data = await response.json();

                    // Handle new notifications
                    if (data.notifications && data.notifications.length > 0) {
                        data.notifications.forEach(notification => {
                            showToastNotification(notification);
                            showBrowserNotification(notification);
                        });
                    }

                    // Update badge count
                    updateBadgeCount(data.unreadCount);
                    lastPollTime = new Date();
                }
            } catch (err) {
                console.error('Polling failed:', err);
            }
        }, config.pollingInterval);
    }

    function stopPolling() {
        if (pollingTimer) {
            clearInterval(pollingTimer);
            pollingTimer = null;
            console.log('Stopped notification polling');
        }
    }

    // ==================== Browser Notification Permission ====================

    function requestNotificationPermission() {
        if ('Notification' in window && Notification.permission === 'default') {
            // Don't request immediately - wait for user interaction
            document.addEventListener('click', function requestOnClick() {
                Notification.requestPermission().then(permission => {
                    console.log('Notification permission:', permission);
                });
                document.removeEventListener('click', requestOnClick);
            }, { once: true });
        }
    }

    // ==================== Utility Functions ====================

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function updateConnectionStatus(status) {
        const indicator = document.getElementById('connectionStatus');
        if (indicator) {
            indicator.className = `connection-status ${status}`;
            indicator.title = `Notification connection: ${status}`;
        }
    }

    // ==================== Public API ====================

    // Expose API for external use
    window.BangaliyanNotifications = {
        refresh: async function () {
            try {
                const response = await fetch('/api/notifications/count');
                if (response.ok) {
                    const data = await response.json();
                    updateBadgeCount(data.count);
                }
            } catch (err) {
                console.error('Failed to refresh notifications:', err);
            }
        },
        markAsRead: async function (notificationId) {
            try {
                await fetch(`/api/notifications/${notificationId}/read`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
            } catch (err) {
                console.error('Failed to mark notification as read:', err);
            }
        },
        markAllAsRead: async function () {
            try {
                await fetch('/api/notifications/read-all', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
                updateBadgeCount(0);
            } catch (err) {
                console.error('Failed to mark all notifications as read:', err);
            }
        }
    };

})();
