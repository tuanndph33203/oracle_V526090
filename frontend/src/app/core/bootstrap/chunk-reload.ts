const CHUNK_RELOAD_KEY = 'chunk-reload';
const CHUNK_RELOAD_QUERY_PARAM = 'chunk-reload';
const CHUNK_ERROR_PATTERNS = [
  'failed to fetch dynamically imported module',
  'error loading dynamically imported module',
  'failed to load module script',
  'importing a module script failed',
  'loading chunk',
  'chunkloaderror',
];

function isDefined(value: string | undefined): value is string {
  return Boolean(value);
}

function collectReasonText(reason: unknown, seen = new WeakSet<object>()): string[] {
  if (reason == null) {
    return [];
  }

  if (typeof reason === 'string') {
    return [reason];
  }

  if (typeof reason === 'number' || typeof reason === 'boolean' || typeof reason === 'bigint') {
    return [String(reason)];
  }

  if (typeof reason !== 'object') {
    return [String(reason)];
  }

  if (seen.has(reason)) {
    return [];
  }

  seen.add(reason);

  if (reason instanceof Error) {
    const cause = (reason as Error & { cause?: unknown }).cause;
    return [
      reason.name,
      reason.message,
      reason.stack,
      ...collectReasonText(cause, seen),
    ].filter(isDefined);
  }

  const record = reason as Record<string, unknown>;
  const parts = [
    ...collectReasonText(record['name'], seen),
    ...collectReasonText(record['message'], seen),
    ...collectReasonText(record['type'], seen),
    ...collectReasonText(record['code'], seen),
    ...collectReasonText(record['stack'], seen),
    ...collectReasonText(record['cause'], seen),
  ];

  if (Array.isArray(record['errors'])) {
    for (const error of record['errors']) {
      parts.push(...collectReasonText(error, seen));
    }
  }

  try {
    parts.push(JSON.stringify(reason));
  } catch {
    // Ignore circular structures and keep collected fields only.
  }

  return parts.filter((part) => part.length > 0);
}

function collectEventTargetText(target: unknown): string[] {
  if (target instanceof HTMLScriptElement) {
    return [target.src, target.tagName].filter(isDefined);
  }

  if (target instanceof HTMLLinkElement) {
    return [target.href, target.tagName].filter(isDefined);
  }

  if (target == null || typeof target !== 'object') {
    return [];
  }

  const record = target as Record<string, unknown>;
  return [
    ...collectReasonText(record['src']),
    ...collectReasonText(record['href']),
    ...collectReasonText(record['tagName']),
  ].filter((part) => part.length > 0);
}

function getChunkErrorText(reason: unknown): string {
  const parts = [...collectReasonText(reason)];

  if (reason != null && typeof reason === 'object') {
    const record = reason as Record<string, unknown>;
    parts.push(...collectReasonText(record['filename']));
    parts.push(...collectReasonText(record['error']));
    parts.push(...collectEventTargetText(record['target']));
    parts.push(...collectEventTargetText(record['currentTarget']));
    parts.push(...collectEventTargetText(reason));
  }

  return parts.filter((part) => part.length > 0).join(' ');
}

function hasChunkLikeAssetUrl(text: string): boolean {
  return /(?:^|\/)(?:chunk|main|polyfills|styles)-[^/\s]+\.(?:js|css)(?:[?#]|$)/i.test(text);
}

function shouldReload(text: string): boolean {
  const normalizedText = text.toLowerCase();

  return (
    CHUNK_ERROR_PATTERNS.some((pattern) => normalizedText.includes(pattern)) ||
    hasChunkLikeAssetUrl(text)
  );
}

function getCacheBustedUrl(): string {
  const url = new URL(location.href);
  url.searchParams.set('ngsw-bypass', 'true');
  url.searchParams.set(CHUNK_RELOAD_QUERY_PARAM, Date.now().toString());

  return url.toString();
}

function cleanReloadParams(): void {
  const url = new URL(location.href);
  let didChange = false;

  for (const param of [CHUNK_RELOAD_QUERY_PARAM, 'ngsw-bypass']) {
    if (url.searchParams.has(param)) {
      url.searchParams.delete(param);
      didChange = true;
    }
  }

  if (didChange) {
    history.replaceState(history.state, document.title, `${url.pathname}${url.search}${url.hash}`);
  }
}

function tryReload(tag: string) {
  const reloadCount = Number(sessionStorage.getItem(CHUNK_RELOAD_KEY) || 0);

  if (reloadCount < 1) {
    sessionStorage.setItem(CHUNK_RELOAD_KEY, String(reloadCount + 1));
    console.warn(`[${tag}] Reloading...`);
    location.replace(getCacheBustedUrl());
  } else {
    console.error(`[${tag}] Reload loop prevented`);
  }
}

export function isChunkLoadError(reason: unknown): boolean {
  return shouldReload(getChunkErrorText(reason));
}

export function resetChunkReloadState(): void {
  sessionStorage.removeItem(CHUNK_RELOAD_KEY);
  cleanReloadParams();
}

export function setupChunkReloadHandler(): void {
  window.addEventListener('error', (e) => {
    const msg = getChunkErrorText(e);
    if (shouldReload(msg)) {
      tryReload('Chunk');
    }
  }, true);

  window.addEventListener('unhandledrejection', (e: PromiseRejectionEvent) => {
    const msg = getChunkErrorText(e.reason);
    if (shouldReload(msg)) {
      tryReload('Chunk async');
    }
  });
}
