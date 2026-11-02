import { initialSystemSettingsState, ISystemSettingsState } from '@/core/models/setting';
import { Action, createReducer, on } from '@ngrx/store';
import SystemSettingsAction from './system-settings.action';

const reducer = createReducer(
    initialSystemSettingsState,

    on(SystemSettingsAction.loadAll, (state) => ({
        ...state,
        loading: true,
        error: null
    })),

    on(SystemSettingsAction.loadAllSuccess, (state, { settings }) => ({
        ...state,
        settings,
        loading: false,
        loaded: true,
        error: null
    })),

    on(SystemSettingsAction.loadAllFailure, (state, { error }) => ({
        ...state,
        loading: false,
        loaded: false,
        error
    })),
);

export function systemSettingsReducer(state: ISystemSettingsState | undefined, action: Action) {
    return reducer(state, action);
}
