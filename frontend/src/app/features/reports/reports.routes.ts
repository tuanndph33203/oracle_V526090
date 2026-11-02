import { Routes } from '@angular/router';

export default [
    {
        path: 'revenue-by-item',
        loadComponent: () => import('./pages/revenue-report/revenue-report.component').then(m => m.RevenueReportComponent)
    },
    {
        path: 'detailed-revenue',
        loadComponent: () => import('./pages/detailed-revenue-report/detailed-revenue-report.component').then(m => m.DetailedRevenueReportComponent)
    },
    {
        path: 'daily-revenue',
        loadComponent: () => import('./pages/daily-revenue-report/daily-revenue-report.component').then(m => m.DailyRevenueReportComponent)
    },
    {
        path: 'invoices',
        loadComponent: () => import('./pages/invoice-list-report/invoice-list-report.component').then(m => m.InvoiceListReportComponent)
    }
] as Routes;
