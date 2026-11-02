export const LAYOUT_CONFIG_KEY = 'app_layout_config';
export const actionUser = [
    {
        label: 'Hồ sơ',
        icon: 'pi pi-user',
        action: 'profile',
        permission: '*'
    },
    {
        label: 'Bàn giao ca',
        icon: 'pi pi-wallet',
        action: 'shift-session',
        permission: '*'
    },
    {
        label: 'Hệ thống',
        icon: 'pi pi-cog',
        action: 'system',
        routerLink: '/systems/settings',
        permission: 'SYSTEM_SETTING_VIEW'
    },
    {
        label: 'Cấu hình máy in Bếp/Bar',
        icon: 'pi pi-print',
        action: 'print-settings',
        routerLink: '/systems/print-settings',
        permission: 'KITCHEN_BAR_PRINT_SETTING_VIEW'
    },
    {
        label: 'Online',
        icon: 'pi pi-users',
        action: 'online',
        permission: '*'
    },
    {
        label: 'Logs',
        icon: 'pi pi-file',
        action: 'logs',
        permission: '*'
    },
    {
        separator: true
    }
];
