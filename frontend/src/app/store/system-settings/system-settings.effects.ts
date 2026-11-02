import { SystemSettingsService } from '@/features/system/settting/services/system-settings';
import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap } from 'rxjs';
import SystemSettingsAction from './system-settings.action';


@Injectable()
export class SystemSettingsEffects {
    private actions = inject(Actions);
    private systemSettingsService = inject(SystemSettingsService);

    loadAll = createEffect(() =>
        this.actions.pipe(
            ofType(SystemSettingsAction.loadAll),
            switchMap(() =>
                this.systemSettingsService.getAll().pipe(
                    map((res) =>
                        SystemSettingsAction.loadAllSuccess({
                            settings: res?.Data ?? []
                        })
                    ),
                    catchError((error) =>
                        of(SystemSettingsAction.loadAllFailure({ error }))
                    )
                )
            )
        )
    );
}