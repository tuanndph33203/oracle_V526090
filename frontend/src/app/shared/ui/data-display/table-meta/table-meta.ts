import { TooltipModule } from 'primeng/tooltip';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, OnInit, output, signal, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Popover } from 'primeng/popover';
import { Table, TableModule } from 'primeng/table';
import { NoData } from '../no-data/no-data';
import { Loading } from '../loading/loading';
import { ITableAction, ITableActionEvent, ITableConfig } from '@/core/models/common';
import { Badge } from 'primeng/badge';
import { ColumnToggle } from '../column-toggle/column-toggle';
import { PaginatorMeta } from '../paginator-meta/paginator-meta';
import { PaginatorState } from 'primeng/paginator';
import { Store } from '@ngrx/store';
import { distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectPermissions } from '@/store/auth/auth.selectors';
import { Image } from 'primeng/image';
import { RenderCellPipe } from '@/shared/pipes/render-cell';
import { ParseDatePipe } from '@/shared/pipes/parse-date';
import { selectSystemSettings } from '@/store/system-settings/system-settings.selectors';
import { ISystemSetting } from '@/core/models/setting';
@Component({
    selector: 'app-table-meta',
    imports: [CommonModule, Popover, ButtonModule, TableModule, Checkbox, FormsModule, TooltipModule, NoData, Loading, Badge, ColumnToggle, PaginatorMeta, Image, RenderCellPipe, ParseDatePipe],
    templateUrl: './table-meta.html',
    styleUrl: './table-meta.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TableMeta implements OnInit {
    @ViewChild('dt') dt!: Table;
    globalFilterFields = computed(() =>
        this.columnsTable()
            .filter((c) => c.visible !== false)
            .map((c) => c.field)
    );
    columnsTable = signal<ITableConfig[]>([]);

    dataTable = input.required<any[]>();
    columns = input.required<ITableConfig[]>();

    rowHeight = input<number>(37);
    totalData = input<number>(0);
    currentPage = input<number>(1);
    pageSize = input<number>(15);
    loading = input<boolean>(true);

    showFooter = input<boolean>(false);
    showNodata = input<boolean>(false);
    showLoading = input<boolean>(false);
    hasActionDbClick = input<boolean>(false);
    showToggleColumn = input<boolean>(true);
    scrollHeight = input<string>('');
    virtualScroll = input<boolean>(false);
    virtualScrollItemSize = input<number>(37);

    showCheckboxToggle = input<boolean>(false);
    selectedRowsInput = input<any[]>([]);
    rowClass = input<((row: any) => string | string[] | Record<string, boolean> | undefined) | null>(null);
    actionClick = output<ITableActionEvent>();
    actionLazyload = output<any>();
    actionDbClick = output<any>();
    actionChangeCheckbox = output<any>();
    actionPageChange = output<PaginatorState>();
    rowCheckboxCondition = input<((row: any) => boolean) | null>(null);
    selectedRows = new Set<any>();
    allSelected = false;

    userPermissions = signal<string[]>([]);
    systemConfigs = signal<ISystemSetting[]>([]);

    protected store = inject(Store);
    protected destroyRef: DestroyRef = inject(DestroyRef);

    constructor() {
        this.store
            .select(selectPermissions)
            .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe((permissions) => {
                this.userPermissions.set(permissions || []);
            });
        this.store
            .select(selectSystemSettings)
            .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
            .subscribe((configs) => {
                this.systemConfigs.set(configs || []);
            });

        effect(() => {
            const cols = this.columns();
            const permissions = this.userPermissions();

            if (!cols?.length) return;

            const normalizedPermissions = permissions?.map(p => p.replace(/\./g, '_').toUpperCase()) || [];

            const filtered = cols.filter((col) => {
                if (!col.permissions?.length) return true;
                return col.permissions.some((p) => {
                    const query = p.replace(/\./g, '_').toUpperCase();
                    return normalizedPermissions.includes(query);
                });
            });

            this.columnsTable.set(filtered);
        });

        effect(() => {
            const rows = this.selectedRowsInput() ?? [];
            this.selectedRows = new Set(rows);
            const dataRows = this.dataTable() ?? [];
            this.allSelected = dataRows.length > 0 && dataRows.every((row) => this.selectedRows.has(row));
        });
    }
    ngOnInit() { }

    formatCurrency(value: number) {
        return value.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });
    }

    getIndex(i: number): number {
        return (this.currentPage() - 1) * this.pageSize() + i + 1;
    }

    toggleSelectAll(event: any) {
        if (event.checked) {
            this.dataTable().forEach((r) => this.selectedRows.add(r));
        } else {
            this.selectedRows.clear();
        }

        this.actionChangeCheckbox.emit({ selectedRows: Array.from(this.selectedRows) });
    }

    toggleRow(row: any, event: any) {
        event.checked ? this.selectedRows.add(row) : this.selectedRows.delete(row);
        this.actionChangeCheckbox.emit({ selectedRows: Array.from(this.selectedRows) });
    }

    totalSelected() {
        return this.selectedRows.size;
    }

    onActionDubleClick(row: any) {
        if (!this.hasActionDbClick()) return;
        this.actionDbClick.emit(row);
    }

    onActionClick(btn: ITableAction, row: any) {
        btn?.command?.();
        this.actionClick.emit({
            action: btn,
            row: row
        });
    }

    onScroll(event: any) {
        this.actionLazyload.emit(event);
    }

    onPageChange(event: PaginatorState) {
        this.actionPageChange.emit(event);
    }

    toggleMenu(event: MouseEvent, popover: any) {
        event.preventDefault();
        event.stopPropagation();
        popover.toggle(event);
    }

    booleanToInt(value: boolean | number): number {
        if (typeof value === 'boolean') {
            return value ? 1 : 0;
        }
        return value;
    }

    renderCell(item: ITableConfig, value: any, row: any): string {
        if (item.transformFn) {
            return item.transformFn(value);
        }

        if (item.transformRowFn) {
            return item.transformRowFn(row);
        }

        return value ?? '';
    }

    getFooterValue(column: ITableConfig): string | number {
        const footer = column.footer;
        if (!footer || footer.visible === false) return '';

        const allRows = this.dataTable() ?? [];
        const rows = footer.rowFilter ? allRows.filter((row) => footer.rowFilter?.(row)) : allRows;
        const targetField = footer.field || column.field;

        switch (footer.type) {
            case 'text':
                return footer.text ?? '';

            case 'count':
                return rows.length;

            case 'sum': {
                const total = rows.reduce((sum, row) => {
                    const value = Number(row?.[targetField] ?? 0);
                    return sum + (isNaN(value) ? 0 : value);
                }, 0);

                return footer.formatter ? footer.formatter(total, rows, column) : (total?.toLocaleString('vi-VN') ?? '0');
            }

            case 'avg': {
                if (!rows.length) return 0;

                const total = rows.reduce((sum, row) => {
                    const value = Number(row?.[targetField] ?? 0);
                    return sum + (isNaN(value) ? 0 : value);
                }, 0);

                const avg = total / rows.length;
                return footer.formatter ? footer.formatter(avg, rows, column) : avg;
            }

            case 'custom': {
                return footer.formatter ? footer.formatter(null, rows, column) : '';
            }

            default:
                return '';
        }
    }

    onColumnChange(visibleFields: string[]): void {
        this.columnsTable.update((cols) =>
            cols.map((col) => ({
                ...col,
                visible: visibleFields.includes(col.field)
            }))
        );
    }

    getRowClass(row: any): string | string[] | Record<string, boolean> | undefined {
        return this.rowClass()?.(row);
    }

    exportCSV() {
        if (!this.dt?.value?.length) return;

        this.dt.exportCSV({
            allValues: true
        });
    }

    parseDate(value: string | null | undefined): Date | null {
        if (!value) return null;
        if (!isNaN(Date.parse(value))) {
            return new Date(value);
        }
        const parts = value.trim().split(' ');
        if (parts.length !== 2) return null;

        const [time, date] = parts;
        const [hour, minute] = time.split(':').map(Number);
        const [day, month, year] = date.split('/').map(Number);

        if ([hour, minute, day, month, year].some(isNaN)) return null;
        return new Date(year, month - 1, day, hour, minute);
    }
    hasViewAction(permission?: string, config?: string): boolean {
        if (!permission) return true;
        const normalizedPermissions = this.userPermissions()?.map(p => p.replace(/\./g, '_').toUpperCase()) || [];
        const query = permission.replace(/\./g, '_').toUpperCase();
        const hasPermission = normalizedPermissions.includes(query);
        const hasConfig = !config || this.systemConfigs()?.some((cfg) => cfg.Code === config);
        return hasPermission && hasConfig;
    }

    getActionConfig(btn: ITableAction, row: any, col: ITableConfig): string | undefined {
        if (!btn.config) return undefined;
        if (typeof btn.config === 'function') {
            return btn.config(row, col);
        }
        return btn.config;
    }

    getActionIcon(btn: ITableAction, row: any, col: ITableConfig): string {
        if (!btn.icon) return '';
        if (typeof btn.icon === 'function') {
            return btn.icon(row, col);
        }
        return btn.icon;
    }

    getActionTooltip(btn: ITableAction, row: any, col: ITableConfig): string {
        if (!btn.tooltipLabel) return '';
        if (typeof btn.tooltipLabel === 'function') {
            return btn.tooltipLabel(row, col);
        }
        return btn.tooltipLabel;
    }

    isActionVisible(btn: ITableAction, row: any, col: ITableConfig): boolean {
        if (!btn.visible) return true;
        if (typeof btn.visible === 'function') {
            return btn.visible(row, col);
        }
        return true;
    }
}
