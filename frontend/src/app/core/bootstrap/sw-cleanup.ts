import { environment } from 'src/environments/environment';

const SW_RELOAD_KEY = 'sw-reload';
const SW_RELOAD_QUERY_PARAM = 'sw-reload';

function getCacheBustedUrl(): string {
  const url = new URL(location.href);
  url.searchParams.set('ngsw-bypass', 'true');
  url.searchParams.set(SW_RELOAD_QUERY_PARAM, Date.now().toString());

  return url.toString();
}

function cleanReloadParams(): void {
  const url = new URL(location.href);

  if (!url.searchParams.has(SW_RELOAD_QUERY_PARAM)) {
    return;
  }

  url.searchParams.delete(SW_RELOAD_QUERY_PARAM);
  url.searchParams.delete('ngsw-bypass');
  history.replaceState(history.state, document.title, `${url.pathname}${url.search}${url.hash}`);
}

export async function cleanupLegacyServiceWorker(): Promise<boolean> {
  if (environment.serviceWorker || !('serviceWorker' in navigator)) {
    return false;
  }

  try {
    const registrations = await navigator.serviceWorker.getRegistrations();
    await Promise.all(registrations.map((registration) => registration.unregister()));

    await new Promise((resolve) => setTimeout(resolve, 50));

    if (navigator.serviceWorker.controller) {
      const hasReloaded = sessionStorage.getItem(SW_RELOAD_KEY);

      if (!hasReloaded) {
        sessionStorage.setItem(SW_RELOAD_KEY, '1');
        console.warn('[SW] Page controlled, reloading once...');
        location.replace(getCacheBustedUrl());
        return true;
      }

      console.error('[SW] Reload loop prevented');
    }

    if (typeof caches !== 'undefined') {
      const cacheKeys = await caches.keys();
      const ngswCacheKeys = cacheKeys.filter((key) => key.startsWith('ngsw:'));

      await Promise.all(ngswCacheKeys.map((key) => caches.delete(key)));

      console.log('[SW] Cleanup done:', {
        registrations: registrations.length,
        caches: ngswCacheKeys.length,
      });
    }

    sessionStorage.removeItem(SW_RELOAD_KEY);
    cleanReloadParams();
  } catch (err) {
    console.error('[SW] Cleanup failed', err);
  }

  return false;
}
