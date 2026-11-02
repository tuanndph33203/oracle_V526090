import { ITableAction, ITableConfig, ITreeTableConfig } from '@/core/models/common';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { Component, computed, input, output, signal } from '@angular/core';
import { TreeNode } from 'primeng/api';
import { Checkbox } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { TreeTableModule } from 'primeng/treetable';
import { NoData } from '../no-data/no-data';
import { Loading } from '../loading/loading';
import { FormsModule } from '@angular/forms';
import { Popover } from 'primeng/popover';
import { ButtonModule } from 'primeng/button';
import { Badge } from 'primeng/badge';
import { TableModule } from "primeng/table";

@Component({
    selector: 'app-tree-table-meta',
    imports: [CommonModule, Popover, ButtonModule, Checkbox, FormsModule, TooltipModule, NgOptimizedImage, NoData, Loading, Badge, TreeTableModule, TableModule],
    templateUrl: './tree-table-meta.html',
    styleUrl: './tree-table-meta.scss'
})
export class TreeTableMeta {
    data = input.required<TreeNode[]>();
    columns = input.required<ITreeTableConfig[]>();

    scrollHeight = input<string>('600px');
    rowHeight = input<number>(37);
    pageSize = input<number>(0);
    currentPage = input<number>(0);

    showCheckboxToggle = input<boolean>(false);
    virtualScroll = input<boolean>(true);
    showLoading = input<boolean>(false);
    showNodata = input<boolean>(false);
    showFooter = input<boolean>(false);
    actionClick = output<{
        action: ITableAction;
        row: any;
    }>();

    selectedRows = new Set<any>();
    allSelected = signal(false);

    visibleColumns = computed(() => this.columns().filter((col) => col.visible !== false));

    getIndex(i: number): number {
        console.log(i);

        if (this.pageSize() === 0) {
            return i + 1;
        }
        console.log((this.currentPage() - 1) * this.pageSize());

        return (this.currentPage() - 1) * this.pageSize() + i + 1;
    }
    toggleSelectAll(event: any) {
        this.allSelected.set(event.checked);

        if (event.checked) {
            this.flattenTree(this.data()).forEach((node) => this.selectedRows.add(node.data?.raw));
        } else {
            this.selectedRows.clear();
        }
    }

    toggleRow(row: any, event: any) {
        event.checked ? this.selectedRows.add(row) : this.selectedRows.delete(row);
    }

    onActionClick(action: ITableAction, row: any) {
        this.actionClick.emit({ action, row });
    }

    booleanToInt(value: boolean | number): number {
        if (typeof value === 'boolean') {
            return value ? 1 : 0;
        }
        return value;
    }

    toggleMenu(event: MouseEvent, popover: any) {
        event.preventDefault();
        event.stopPropagation();
        popover.toggle(event);
    }

    getFooterValue(column: ITreeTableConfig): string | number {
        const footer = column.footer;
        if (!footer || footer.visible === false) return '';

        const allRows = this.data() ?? [];
        const rows = footer.rowFilter ? allRows.filter((row) => footer.rowFilter?.(row)) : allRows;
        const targetField = footer.field || column.field;

        switch (footer.type) {
            case 'text':
                return footer.text ?? '';

            case 'count':
                return rows.length;

            case 'sum': {
                const total = rows.reduce((sum, row) => {
                    const source = row?.data as Record<string, unknown> | undefined;
                    const value = Number(source?.[targetField] ?? 0);
                    return sum + (isNaN(value) ? 0 : value);
                }, 0);

                return footer.formatter ? footer.formatter(total, rows, column) : (total?.toLocaleString('vi-VN') ?? '0');
            }

            case 'avg': {
                if (!rows.length) return 0;

                const total = rows.reduce((sum, row) => {
                    const source = row?.data as Record<string, unknown> | undefined;
                    const value = Number(source?.[targetField] ?? 0);
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

    getActionIcon(btn: ITableAction, row: any, col?: ITreeTableConfig): string {
        if (!btn.icon) return '';
        if (typeof btn.icon === 'function') {
            return btn.icon(row, col as any);
        }
        return btn.icon;
    }

    getActionTooltip(btn: ITableAction, row: any, col?: ITreeTableConfig): string {
        if (!btn.tooltipLabel) return '';
        if (typeof btn.tooltipLabel === 'function') {
            return btn.tooltipLabel(row, col as any);
        }
        return btn.tooltipLabel;
    }

    private flattenTree(nodes: TreeNode[]): TreeNode[] {
        let result: TreeNode[] = [];

        for (const node of nodes) {
            result.push(node);
            if (node.children?.length) {
                result = result.concat(this.flattenTree(node.children));
            }
        }
        return result;
    }
}
