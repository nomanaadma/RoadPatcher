/**
 * notification-hub.js
 * Real-time notification updates via SignalR
 * Handles connection to NotificationHub and updates navbar badge + toast notifications
 */

(function () {
    'use strict';

    // Check if SignalR is available
    if (typeof signalR === 'undefined') {
        console.warn('SignalR library not loaded. Real-time notifications disabled.');
        return;
    }

    // Build SignalR hub connection
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notification')
        .withAutomaticReconnect([0, 1000, 5000, 10000]) // Reconnect delays (ms)
        .build();

    // DOM elements
    const badgeElement = document.getElementById('unreadNotificationCount');
    const notificationLink = document.querySelector('a[href="/Notifications"]');

    /**
     * Update the notification badge with count
     * Shows/hides based on count
     */
    function updateBadge(count) {
        if (!badgeElement) return;

        if (count > 0) {
            badgeElement.textContent = count;
            badgeElement.style.display = 'inline-block';
        } else {
            badgeElement.textContent = '0';
            badgeElement.style.display = 'none';
        }
    }

    /**
     * Show toast notification for new or updated notifications
     */
    function showToast(count) {
        if (typeof toastr === 'undefined') return;

        const message = count === 1 
            ? 'You have 1 new notification'
            : `You have ${count} new notifications`;

        toastr.info(message, 'Notifications', {
            timeOut: 5000,
            positionClass: 'toast-bottom-right'
        });
    }

    /**
     * Handle incoming notification count updates from hub
     */
    connection.on('NotificationCountUpdated', function (unreadCount) {
        console.log('NotificationCountUpdated:', unreadCount);
        updateBadge(unreadCount);
        
        // Only show toast if count > 0 and we're not on the notifications page
        if (unreadCount > 0 && !window.location.pathname.includes('/Notifications')) {
            showToast(unreadCount);
        }
    });

    /**
     * Handle connection errors
     */
    connection.onreconnecting((error) => {
        console.warn('SignalR reconnecting...', error);
    });

    connection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId);
    });

    connection.onclose((error) => {
        console.warn('SignalR disconnected:', error);
    });

    /**
     * Fetch initial unread count and then start connection
     */
    async function initializeNotifications() {
        try {
            // Start the SignalR connection
            await connection.start();
            console.log('SignalR connected');

            // Fetch initial unread count (SignalR will also send this on connect)
            const response = await fetch('/Notifications/GetUnreadCount');
            if (response.ok) {
                const data = await response.json();
                updateBadge(data.unreadCount);
            }
        } catch (error) {
            console.error('Failed to connect to SignalR:', error);
            // Fallback: Try polling-based updates every 30 seconds
            pollNotificationCount();
        }
    }

    /**
     * Polling fallback for browsers without WebSocket support
     */
    function pollNotificationCount() {
        setInterval(async () => {
            try {
                const response = await fetch('/Notifications/GetUnreadCount');
                if (response.ok) {
                    const data = await response.json();
                    updateBadge(data.unreadCount);
                }
            } catch (error) {
                console.warn('Failed to poll notification count:', error);
            }
        }, 30000); // Check every 30 seconds
    }

    // Initialize on page load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeNotifications);
    } else {
        initializeNotifications();
    }

    // Expose connection for debugging
    window.notificationHub = {
        connection: connection,
        updateBadge: updateBadge
    };
})();
