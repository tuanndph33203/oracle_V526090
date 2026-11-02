import { ISystemSettingsState } from "@/core/models/setting";

export const initialSystemSettingsState: ISystemSettingsState = {
    settings: [],
    loading: false,
    loaded: false,
    error: null
};