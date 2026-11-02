import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, LOCALE_ID, importProvidersFrom } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter, withEnabledBlockingInitialNavigation, withInMemoryScrolling } from '@angular/router';
import Aura from '@primeuix/themes/aura';
import { providePrimeNG } from 'primeng/config';
import { appRoutes } from './app.routes';
import { authInterceptor } from '@/core/interceptors/auth';

import { JWT_OPTIONS, JwtModule } from '@auth0/angular-jwt';
import { jwtOptionsFactory } from '@/core/services/jwtOptionsFactory';
import { provideState, provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { AuthEffects } from '@/store/auth/auth.effects';
import { authReducer } from '@/store/auth/auth.reducer';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { branchFilterReducer } from '@/store/branch-filter/branch-filter.reducer';
import { MessageService } from 'primeng/api';
import { systemSettingsReducer } from '@/store/system-settings/system-settings.reducer';
import { SystemSettingsEffects } from '@/store/system-settings/system-settings.effects';

export const appConfig: ApplicationConfig = {
    providers: [
        provideRouter(
            appRoutes,
            withInMemoryScrolling({
                anchorScrolling: 'enabled',
                scrollPositionRestoration: 'enabled'
            }),
            withEnabledBlockingInitialNavigation()
        ),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideAnimationsAsync(),
        providePrimeNG({
            theme: { preset: Aura, options: { darkModeSelector: '.app-dark' } },
            translation: {
                firstDayOfWeek: 1,
                dayNames: ['Chủ nhật', 'Thứ hai', 'Thứ ba', 'Thứ tư', 'Thứ năm', 'Thứ sáu', 'Thứ bảy'],
                dayNamesShort: ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'],
                dayNamesMin: ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'],
                monthNames: ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'],
                monthNamesShort: ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10', 'T11', 'T12'],
                today: 'Hôm nay',
                clear: 'Xóa',
                dateFormat: 'dd/mm/yy'
            }
        }),
        importProvidersFrom(
            JwtModule.forRoot({
                jwtOptionsProvider: {
                    provide: JWT_OPTIONS,
                    useFactory: jwtOptionsFactory
                }
            })
        ),
        { provide: LOCALE_ID, useValue: 'vi-VN' },
        provideStore(),
        provideState('auth', authReducer),
        provideState('branchFilter', branchFilterReducer),
        provideState('systemSettings', systemSettingsReducer),
        provideEffects([AuthEffects, SystemSettingsEffects]),
        provideStoreDevtools(),
        MessageService
    ]
};
