import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ISystemSettingsState } from '@/core/models/setting';

export const selectSystemSettingsState = createFeatureSelector<ISystemSettingsState>('systemSettings');

export const selectSystemSettings = createSelector(selectSystemSettingsState, (state) => state.settings);