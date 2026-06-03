// PWA Install & Push Notification Manager
(function() {
    let deferredPrompt;
    let swRegistration;

    console.log('🚀 PWA: Inicializando...');

    // ⭐ NUEVO: Detectar si la app ya está instalada
    function isAppInstalled() {
        // Método 1: Verificar si está corriendo como PWA (standalone)
        if (window.matchMedia('(display-mode: standalone)').matches) {
            console.log('✅ PWA: App está corriendo en modo standalone');
            return true;
        }

        // Método 2: Verificar si está en fullscreen (iOS)
        if (window.navigator.standalone === true) {
            console.log('✅ PWA: App está corriendo en modo fullscreen (iOS)');
            return true;
        }

        // Método 3: Verificar flag guardado en localStorage
        if (localStorage.getItem('pwa-installed') === 'true') {
            console.log('✅ PWA: Flag de instalación encontrado en localStorage');
            return true;
        }

        return false;
    }

    // Register Service Worker
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/service-worker.js')
            .then(reg => {
                console.log('✅ Service Worker registrado correctamente');
                swRegistration = reg;

                // Check for updates every hour
                setInterval(() => {
                    console.log('🔄 Verificando actualizaciones del SW...');
                    reg.update();
                }, 3600000);
            })
            .catch(err => console.error('❌ Error registrando SW:', err));
    } else {
        console.warn('⚠️ Service Workers no soportados en este navegador');
    }

    // PWA Install Prompt
    window.addEventListener('beforeinstallprompt', (e) => {
        console.log('🎯 PWA: Evento beforeinstallprompt capturado');

        // ⭐ NUEVO: Si la app ya está instalada, NO mostrar banner
        if (isAppInstalled()) {
            console.log('ℹ️ PWA: App ya instalada, no mostrar banner');
            e.preventDefault();
            return;
        }

        e.preventDefault();
        deferredPrompt = e;

        // Check if user already dismissed
        const dismissed = localStorage.getItem('pwa-install-dismissed');
        if (dismissed) {
            const dismissTime = parseInt(dismissed);
            const daysSince = (Date.now() - dismissTime) / (1000 * 60 * 60 * 24);
            if (daysSince < 7) {
                console.log('ℹ️ PWA: Banner descartado hace menos de 7 días, no mostrar');
                return;
            }
        }

        showInstallPrompt();
    });

    // Show custom install prompt
    function showInstallPrompt() {
        console.log('📱 PWA: Mostrando banner de instalación');

        // Remove existing banner if any
        const existing = document.getElementById('pwa-install-banner');
        if (existing) existing.remove();

        const banner = document.createElement('div');
        banner.id = 'pwa-install-banner';
        banner.innerHTML = `
            <div style="position:fixed;bottom:20px;left:20px;right:20px;background:#fff;padding:16px;border-radius:12px;box-shadow:0 4px 12px rgba(0,0,0,0.15);z-index:9999;display:flex;align-items:center;gap:12px;max-width:400px;margin:0 auto;animation:slideUp 0.3s ease;">
                <img src="/img/icons/icon-72x72.png" width="48" height="48" style="border-radius:8px;" onerror="this.src='data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%2248%22 height=%2248%22><rect width=%2248%22 height=%2248%22 fill=%22%23764ba2%22/></svg>'" />
                <div style="flex:1;">
                    <strong style="display:block;color:#0f172a;font-size:14px;">Instalar EIIBD como un APP</strong>
                    <span style="color:#6b7280;font-size:12px;">Acceso rápido con Notificaciones</span>
                </div>
                <button id="pwa-install-btn" style="background:#6a4e7a;color:#fff;border:none;padding:8px 16px;border-radius:6px;font-size:13px;font-weight:600;cursor:pointer;white-space:nowrap;">Instalar</button>
                <button id="pwa-dismiss-btn" style="background:transparent;border:none;color:#6b7280;font-size:20px;cursor:pointer;padding:4px 8px;line-height:1;">×</button>
            </div>
            <style>
                @keyframes slideUp {
                    from { transform: translateY(100px); opacity: 0; }
                    to { transform: translateY(0); opacity: 1; }
                }
            </style>
        `;
        document.body.appendChild(banner);

        document.getElementById('pwa-install-btn').addEventListener('click', async () => {
            console.log('👆 PWA: Usuario hizo clic en Instalar');
            if (!deferredPrompt) {
                console.warn('⚠️ PWA: deferredPrompt no disponible');
                return;
            }
            deferredPrompt.prompt();
            const { outcome } = await deferredPrompt.userChoice;
            console.log(`📊 PWA install outcome: ${outcome}`);

            // ⭐ NUEVO: Si aceptó instalar, marcar como instalado
            if (outcome === 'accepted') {
                console.log('✅ PWA: Usuario aceptó instalar');
                localStorage.setItem('pwa-installed', 'true');
            }

            deferredPrompt = null;
            banner.remove();
        });

        document.getElementById('pwa-dismiss-btn').addEventListener('click', () => {
            console.log('❌ PWA: Usuario descartó el banner');
            banner.remove();
            localStorage.setItem('pwa-install-dismissed', Date.now().toString());
        });

        // Auto-dismiss after 15 seconds
        setTimeout(() => {
            if (banner.parentNode) {
                console.log('⏱️ PWA: Auto-descartando banner después de 15s');
                banner.remove();
            }
        }, 15000);
    }

    // Handle app installed
    window.addEventListener('appinstalled', () => {
        console.log('✅ PWA instalada correctamente');

        // ⭐ NUEVO: Marcar como instalada en localStorage
        localStorage.setItem('pwa-installed', 'true');

        // Remover banner si aún está visible
        const banner = document.getElementById('pwa-install-banner');
        if (banner) banner.remove();

        deferredPrompt = null;

        // Auto-subscribe to push notifications after install
        if ('Notification' in window && Notification.permission === 'default') {
            console.log('🔔 Solicitando permiso de notificaciones...');
            setTimeout(() => {
                window.subscribeToPush();
            }, 25000);
        }
    });

    // Push Notification Functions
    window.subscribeToPush = async function() {
        console.log('🔔 [subscribeToPush] Iniciando...');

        if (!swRegistration) {
            console.error('❌ [subscribeToPush] Service Worker no disponible');
            alert('Service Worker no está registrado. Recarga la página e intenta de nuevo.');
            return null;
        }

        try {
            console.log('🔔 [subscribeToPush] Solicitando permiso...');
            const permission = await Notification.requestPermission();
            console.log(`🔔 [subscribeToPush] Permiso resultado: ${permission}`);

            if (permission !== 'granted') {
                console.log('⚠️ [subscribeToPush] Permiso denegado o descartado');
                return null;
            }

            // Get VAPID public key from server
            console.log('🔑 [subscribeToPush] Obteniendo VAPID public key de /api/push/vapid-public-key...');
            const response = await fetch('/api/push/vapid-public-key');

            if (!response.ok) {
                console.error('❌ [subscribeToPush] Error obteniendo VAPID key:', response.status, response.statusText);
                throw new Error(`Error obteniendo VAPID key: ${response.status} ${response.statusText}`);
            }

            const vapidPublicKey = await response.text();
            console.log('✅ [subscribeToPush] VAPID key obtenida (length: ' + vapidPublicKey.length + ')');

            console.log('📝 [subscribeToPush] Suscribiendo a push manager...');
            const subscription = await swRegistration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
            });

            console.log('✅ [subscribeToPush] Suscripción creada:', subscription.endpoint.substring(0, 50) + '...');

            console.log('💾 [subscribeToPush] Enviando suscripción al servidor /api/push/subscribe...');
            const saveResponse = await fetch('/api/push/subscribe', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(subscription)
            });

            if (!saveResponse.ok) {
                const errorText = await saveResponse.text();
                console.error('❌ [subscribeToPush] Error guardando suscripción:', saveResponse.status, errorText);
                throw new Error(`Error guardando suscripción: ${saveResponse.status} - ${errorText}`);
            }

            const saveResult = await saveResponse.json();
            console.log('✅ [subscribeToPush] Suscripción guardada en servidor:', saveResult);
            console.log('✅ [subscribeToPush] ¡Proceso completado exitosamente!');

            return subscription;
        } catch (error) {
            console.error('❌ [subscribeToPush] Error:', error);
            console.error('❌ [subscribeToPush] Stack:', error.stack);
            throw error;
        }
    };

    // Helper function
    function urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/\-/g, '+').replace(/_/g, '/');
        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);
        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    // Debug: Check PWA status
    console.log('📊 PWA Status Check:');
    console.log('- Service Worker support:', 'serviceWorker' in navigator);
    console.log('- Notification support:', 'Notification' in window);
    console.log('- Push support:', 'PushManager' in window);
    console.log('- Notification permission:', 'Notification' in window ? Notification.permission : 'N/A');
})();
