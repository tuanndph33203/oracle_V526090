export interface ISettingDefinition {
    Code: string;
    Name: string;
    Value: string;
    DataType: string;
    Description: string;
    IsActive: boolean;
}

export interface ISettingItem extends ISettingDefinition {
    Id?: string;
    Module: string;
    _checked?: boolean;
    _mapped?: boolean;
    _multiValues?: any[];
}

export interface ISettingGroupDefinition {
    module: string;
    label: string;
    icon: string;
    description: string;
    items: ISettingDefinition[];
}

export interface ISettingGroup {
    module: string;
    label: string;
    icon: string;
    description: string;
    items: ISettingItem[];
}
