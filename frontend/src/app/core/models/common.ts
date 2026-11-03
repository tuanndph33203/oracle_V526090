import { MenuItem } from 'primeng/api';
import { KeyOfType } from './layout';
import { presets } from '../constants/config';

export interface ITableConfig {
    label: string;
    header?: string;
    field: string;

    type?: 'index' | 'text' | 'date' | 'dateHour' | 'image' | 'transform' | 'currency' | 'number' | 'select' | 'severity' | 'action';

    align?: 'right' | 'left' | 'center';

    visible: boolean;

    transformFn?: (v: any) => any;
    transformRowFn?: (v: any) => any;

    width?: string;
    minWidth?: string;
    maxWidth?: string;
    config?: string;
    tooltip?: boolean;
    tooltipField?: string;

    truncate?: number;
    lineClamp?: 1 | 2 | 3 | 4 | 5;
    sortable?: boolean;

    badge?: ITableBadgeConfig;

    footer?: {
        type?: FooterType;
        text?: string;
        field?: string;
        formatter?: (value: any, rows: any[], column: ITableConfig) => string;
        rowFilter?: (row: any) => boolean;
        visible?: boolean;
        class?: string;
    };

    severity?: Record<string, SeverityConfig>;

    permissions?: string[];

    actionButtons?: Array<ITableAction>;
}

export type FooterType = 'text' | 'sum' | 'count' | 'avg' | 'custom';

export type Severity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';
export interface SeverityConfig {
    label?: string;
    severity?: Severity;
    color?: string;
}

export interface ITableBadgeConfig {
    value?: string | number | ((row: any) => string | number);

    severity?: Severity | ((row: any) => Severity);

    badgeDisabled?: boolean | ((row: any) => boolean);
}
export interface ITableAction {
    label?: string;
    command?: () => void;
    icon?: string | ((row: any, col?: ITableConfig) => string);
    type: string;
    severity?: Severity | ((row: any, col?: ITableConfig) => Severity);
    permission?: string;
    tooltipLabel?: string | ((row: any, col?: ITableConfig) => string);
    children?: ITableAction[];
    config?: (row: any, col: ITableConfig) => string | undefined
    visible?: (row: any, col: ITableConfig) => boolean;
}
export interface ApiResponse<T> {
    Code: number;
    Message: string;
    TotalTime: number;
    Data: PageData<T>;
    DataCount?: number | null;
    Status?: string | null;
    TotalCount?: number | null;
}

export interface ApiResponseDetail<T> {
    Data: T;
    Code: number;
    Message: string;
    TotalTime: number;
    DataCount: number | null;
    Status: string | null;
    TotalCount: number | null;
}
export interface PageData<T> {
    Page: number;
    Size: number;
    TotalPages: number;
    TotalElements: number;
    NumberOfElements: number;
    Content: T[];
}

export interface ITableActionEvent<Row = any> {
    action: ITableAction;
    row: Row;
}

export interface LazyLoadEventData {
    first: number;
    rows: number;
    sortOrder?: 1 | -1 | 0; // tăng | giảm | none
    filters?: Record<string, any>;
    globalFilter?: string | null;
}

export interface AppMenuItem extends MenuItem {
    _active?: boolean;
    _hasActiveDescendant?: boolean;
    _rootActive?: boolean;
}
export type PresetKey = KeyOfType<typeof presets>;

export interface PrimeIconItem {
    key: string;
    class: string;
    library?: 'prime' | 'phosphor';
}

export interface FormMapper<TForm, TPayload, TApi> {
    apiToForm(data: TApi): Partial<TForm>;
    formToPayload(form: TForm, edition: boolean, id?: string): TPayload;
}

export interface ITreeTableConfig {
    field: string;
    label: string;
    type: 'tree' | 'index' | 'text' | 'date' | 'image' | 'transform' | 'currency' | 'number' | 'select' | 'severity' | 'action';

    treeToggle?: boolean;
    width?: string;
    minWidth?: string;
    maxWidth?: string;
    align?: 'left' | 'center' | 'right';

    transformFn?: (v: any) => any;
    transformRowFn?: (v: any) => any;

    iconField?: string;
    lineClamp?: number;

    tooltip?: boolean;
    tooltipField?: string;

    severity?: Record<string, SeverityConfig>;

    actionButtons?: ITableAction[];

    visible?: boolean;

    footer?: {
        type?: FooterType;
        text?: string;
        field?: string;
        formatter?: (value: any, rows: any[], column: ITreeTableConfig) => string;
        rowFilter?: (row: any) => boolean;
        visible?: boolean;
        class?: string;
    };
}
