export interface ISystemSetting {
    Id?: number;
    Code: string;
    Name?: string;
    Value?: string;
    Description?: string;
    IsActive: boolean;
}

export interface ISystemSettingsState {
    settings: ISystemSetting[];
    loading: boolean;
    loaded: boolean;
    error: any;
}

export const initialSystemSettingsState: ISystemSettingsState = {
    settings: [],
    loading: false,
    loaded: false,
    error: null
};