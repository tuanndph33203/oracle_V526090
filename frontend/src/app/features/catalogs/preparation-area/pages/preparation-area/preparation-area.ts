import { ITableAction, ITableConfig } from '@/core/models/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { debounceTime, distinctUntilChanged, startWith } from 'rxjs';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { PaginatorMeta } from '@/shared/ui/data-display/paginator-meta/paginator-meta';
import { HasPermissionDirective } from "@/shared/directive/hasPermission";
import { PreparationAreaService } from '../../services/preparation-area';
import { colsTempPreparationArea } from '../../contants/preparation-area';
import { PreparationAreaCreate } from '../../components/preparation-area-create/preparation-area-create';

@Component({
    selector: 'app-preparation-area',
    imports: [CommonModule, TableMeta, PaginatorMeta, ButtonModule, InputTextModule, InputIcon, IconField, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './preparation-area.html',
    styleUrl: './preparation-area.scss'
})
export class PreparationArea extends BaseCrudPage<any> implements OnInit {

    colsTemp = signal<ITableConfig[]>(colsTempPreparationArea);

    protected service = inject(PreparationAreaService);
    protected formComponent = PreparationAreaCreate;
    protected entityName = 'Bếp/Bar';

    override onCreate() {
        this.dialogService.open(this.formComponent, {
            showHeader: false,
            width: '24rem',
            breakpoints: { '640px': 'calc(100vw - 2rem)' },
            contentStyle: { padding: '0', overflow: 'hidden' },
            styleClass: 'preparation-area-dialog'
        })?.onClose.subscribe((ok) => ok && this.search({ BranchCode: this.BranchCode(), ...this.searchQuery() }));
    }

    override onEdit(row: any) {
        this.dialogService.open(this.formComponent, {
            showHeader: false,
            width: '24rem',
            breakpoints: { '640px': 'calc(100vw - 2rem)' },
            contentStyle: { padding: '0', overflow: 'hidden' },
            styleClass: 'preparation-area-dialog',
            data: { edition: true, data: row }
        })?.onClose.subscribe((ok) => ok && this.search({ BranchCode: this.BranchCode(), ...this.searchQuery() }));
    }

    formSearch = new FormGroup({
        TextSearch: new FormControl('')
    });

    ngOnInit() {
        this.formSearch.valueChanges
            .pipe(
                debounceTime(600),
                startWith(this.formSearch.value),
                distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b))
            )
            .subscribe((filters) => {
                this.onSearch(filters);
            });
    }

    protected override mapResponseItems(items: any[]): any[] {
        return items.map((x: any) => ({
            ...x,
            TypeText: this.getTypeText(x.Type),
            DeviceTypeText: this.getDeviceTypeText(x.DeviceType),
            LabelPrintTimingText: this.getLabelPrintTimingText(x.LabelPrintTiming),
            StatusText: x.Status === 1 ? 'Đang hoạt động' : (x.Status === 2 ? 'Ngừng sử dụng' : 'Đã xóa')
        }));
    }

    private getTypeText(value: number): string {
        return ({ 1: 'Bếp', 2: 'Bar', 3: 'Cả hai' } as Record<number, string>)[value] ?? '—';
    }

    private getDeviceTypeText(value: number): string {
        return ({ 1: 'Tablet (Màn hình KDS)', 2: 'Printer (Máy in bill)', 3: 'Không thiết bị' } as Record<number, string>)[value] ?? '—';
    }

    private getLabelPrintTimingText(value: number | null): string {
        if (value == null) return '—';
        return ({ 1: 'Sau khi chế biến', 2: 'Sau khi trả món' } as Record<number, string>)[value] ?? '—';
    }

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEdit(row);
        if (action.type === 'delete') this.onDelete(row);
    }
}
