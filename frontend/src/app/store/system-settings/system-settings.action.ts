import { ISystemSetting } from '@/core/models/setting';
import { createAction, props } from '@ngrx/store';

const SystemSettingsAction = {
    loadAll: createAction('[System Settings] Load All'),
    loadAllSuccess: createAction('[System Settings] Load All Success', props<{ settings: ISystemSetting[] }>()),
    loadAllFailure: createAction('[System Settings] Load All Failure', props<{ error: any }>()),
};

export default SystemSettingsAction;
