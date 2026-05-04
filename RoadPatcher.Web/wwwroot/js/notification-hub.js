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



    async function getPendingToasts() {
        try {
            const response = await fetch('/Notifications/GetPendingToasts');
            if (!response.ok) return [];

            return await response.json();
        } catch (error) {
            console.warn('Failed to load pending toasts:', error);
            return [];
        }
    }

    async function markToastAsSeen(notificationId) {
        try {
            const formData = new FormData();
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

            if (tokenInput) {
                formData.append('__RequestVerificationToken', tokenInput.value);
            }

            const response = await fetch(`/Notifications/MarkToastAsSeen/${notificationId}`, {
                method: 'POST',
                body: formData
            });

            return response.ok;
        } catch (error) {
            console.warn('Failed to mark toast as seen:', error);
            return false;
        }
    }

    let showingToastIds = new Set(); // Track which notifications already have toasts showing

    async function showPendingToasts() {
        try {
            const notifications = await getPendingToasts();
            if (!notifications.length) {
                console.log('No pending notifications.');
                return;
            }

            if (typeof toastr === 'undefined') {
                console.warn('Toastr not loaded.');
                return;
            }

            // Show a toast for each notification that doesn't already have one
            notifications.forEach((notification) => {
                if (showingToastIds.has(notification.id)) {
                    console.log('Toast already shown for notification:', notification.id);
                    return; // Skip if we've already shown a toast for this notification
                }

                console.log('Showing toast for notification:', notification.id);
                showingToastIds.add(notification.id);

                const $toast = toastr.info('You have a new notification', '', {
                    timeOut: 3000,
                    extendedTimeOut: 3000,
                    closeButton: true,
                    tapToDismiss: true,
                    positionClass: 'toastr-bottom-right',
                    onclick: async function () {
                        console.log('Toast clicked, marking as seen:', notification.id);
                        // mark this notification's toast as seen via AJAX
                        const ok = await markToastAsSeen(notification.id);
                        if (ok) {
                            console.log('Successfully marked as seen:', notification.id);
                        } else {
                            console.warn('Failed to mark as seen:', notification.id);
                        }

                        toastr.clear($toast);
                        showingToastIds.delete(notification.id);
                    }
                });
            });
        } catch (error) {
            console.error('Error in showPendingToasts:', error);
        }
    }

    /**
     * Handle incoming notification count updates from hub
     */
    connection.on('NotificationCountUpdated', function (unreadCount) {
        console.log('NotificationCountUpdated:', unreadCount);
        updateBadge(unreadCount);
        // Don't show toasts on every count update - only on initial load via initializeNotifications()
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
                updateBadge(data.unread_count);
            }

            if (!window.location.pathname.includes('/Notifications')) {
                await showPendingToasts();
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
                    updateBadge(data.unread_count);
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
