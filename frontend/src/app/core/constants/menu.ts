export const menu_nav_ = [
    {
        label: 'Tổng quan',
        icon: 'pi pi-chart-line',
        routerLink: ['/']
    },
    {
        label: 'Danh mục',
        icon: 'pi pi-list',
        items: [
            {
                label: 'Sản phẩm',
                icon: 'pi pi-box',
                routerLink: ['catalogs/product']
            },
            {
                label: 'Đơn vị tính',
                icon: 'pi pi-circle-off',
                routerLink: ['catalogs/unit']
            },
            {
                label: 'Nhóm sản phẩm',
                icon: 'pi pi-circle-off',
                routerLink: ['catalogs/product-group']
            },
            {
                label: 'Loại sản phẩm',
                icon: 'pi pi-circle-off',
                routerLink: ['catalogs/product-type']
            }
        ]
    },
    {
        label: 'Hệ thống',
        icon: 'pi pi-fw pi-database',
        items: [
            {
                label: 'Tài khoản',
                icon: 'pi pi-fw pi-users',
                routerLink: ['systems/account']
            },
            {
                label: 'Quyền',
                icon: 'pi pi-fw pi-id-card',
                routerLink: ['systems/permission']
            },
            {
                label: 'Điều hướng',
                icon: 'pi pi-fw pi-sitemap',
                routerLink: ['systems/navigation']
            },
            {
                label: 'Cấu hình hệ thống',
                icon: 'pi pi-fw pi-cog',
                routerLink: ['systems/settings']
            }
        ]
    }
];
