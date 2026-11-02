import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app.config';
import { AppComponent } from './app.component';
import { registerLocaleData } from '@angular/common';
import localeVi from '@angular/common/locales/vi';
import { cleanupLegacyServiceWorker } from '@/core/bootstrap/sw-cleanup';
import { isChunkLoadError, resetChunkReloadState, setupChunkReloadHandler } from '@/core/bootstrap/chunk-reload';

export async function bootstrapApp(): Promise<void> {
  registerLocaleData(localeVi);
  setupChunkReloadHandler();

  const didTriggerReload = await cleanupLegacyServiceWorker();

  if (didTriggerReload) {
    return;
  }

  try {
    await bootstrapApplication(AppComponent, appConfig);
    resetChunkReloadState();
  } catch (err) {
    if (!isChunkLoadError(err)) {
      resetChunkReloadState();
    }

    throw err;
  }
}

bootstrapApp().catch((err) => console.error(err));
