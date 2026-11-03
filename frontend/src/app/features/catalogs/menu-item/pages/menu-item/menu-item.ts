import { ITableAction, ITableConfig } from '@/core/models/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { debounceTime, distinctUntilChanged, finalize, startWith } from 'rxjs';
import { map } from 'rxjs/operators';
import { MenuItemCreate } from '../../components/menu-item-create/menu-item-create';
import { colsTempMenuItem } from '../../contants/menu-item';
import { MenuItemService } from '../../services/menu-item';
import { MenuGroupService } from '../../../menu-group/services/menu-group';
import { BaseCrudPage } from '@/shared/base/base-list-page';
import { HasPermissionDirective } from '@/shared/directive/hasPermission';
import { PaginatorMeta } from '@/shared/ui/data-display/paginator-meta/paginator-meta';
import { cleanObject } from '@/core/utils/cleanQuery';

@Component({
    selector: 'app-menu-item',
    imports: [CommonModule, TableMeta, ButtonModule, InputTextModule, InputIcon, IconField, SelectModule, MultiSelectModule, ReactiveFormsModule, HasPermissionDirective],
    templateUrl: './menu-item.html',
    styleUrl: './menu-item.scss'
})
export class MenuItem extends BaseCrudPage<any> implements OnInit {
    colsTemp = signal<ITableConfig[]>(colsTempMenuItem);

    protected service = inject(MenuItemService);
    private menuGroupService = inject(MenuGroupService);
    protected formComponent = MenuItemCreate;
    protected entityName = 'Thực đơn';

    formSearch = new FormGroup({
        TextSearch: new FormControl(''),
        MenuGroupIds: new FormControl<string[]>([], { nonNullable: true }),
        IsInactive: new FormControl<boolean | null>(null),
        IsAvailable: new FormControl<boolean | null>(null)
    });

    menuGroupOptions = signal<{ Id: string; Name: string }[]>([]);
    statusOptions = [
        { label: 'Đang hoạt động', value: false },
        { label: 'Ngừng sử dụng', value: true }
    ];
    availabilityOptions = [
        { label: 'Hiển thị (Còn món)', value: true },
        { label: 'Tắt (Hết món)', value: false }
    ];

    constructor() {
        super();
        const originalGetByFilter = this.service.getByFilter.bind(this.service);
        (this.service as any).getByFilter = (query: any) => {
            return originalGetByFilter(query).pipe(
                map((res: any) => {
                    if (res?.Code === 200) {
                        const content = res.Data?.Content || res.Data?.Items || res.Data;
                        if (Array.isArray(content)) {
                            const mapped = content.map((x: any) => {
                                const branches = x.BranchIds || [];
                                const branchNames = branches.map((id: string) => {
                                    const b = this.branchUser().find(bo => bo.Id?.toString().toLowerCase() === id.toString().toLowerCase());
                                    return b ? b.Name : null;
                                }).filter(Boolean);
                                const branchDisplay = branchNames.length > 0 ? branchNames.join(', ') : 'Tất cả chi nhánh';
                                const taxPercent = x.TaxRatePercent || 0;
                                const fixedTax = x.FixedTaxAmount || 0;
                                let totalPrice = 0;
                                if (fixedTax > 0) {
                                    totalPrice = (x.SalePrice || 0) + fixedTax;
                                } else if (taxPercent > 0) {
                                    totalPrice = (x.SalePrice || 0) * (1 + taxPercent / 100);
                                } else {
                                    totalPrice = x.SalePrice || 0;
                                }
                                return {
                                    ...x,
                                    MenuGroupDisplay: x.MenuGroupName ?? '',
                                    UnitDisplay: x.UnitName ?? '',
                                    TaxRateDisplay: x.TaxRatePercent != null ? `${x.TaxRatePercent}%` : '0%',
                                    StatusText: x.Status === 1 ? 'Đang hoạt động' : x.Status === 2 ? 'Ngừng sử dụng' : 'Đã xóa',
                                    IsAvailableDisplay: x.IsAvailable !== false 
                                        ? (x.IsTrackStock ? `🟢 Còn món (Còn ${x.StockQuantity ?? 0})` : '🟢 Còn món') 
                                        : '🔴 Hết món',
                                    BranchDisplay: branchDisplay,
                                    SalePriceDisplay: Math.round(x.SalePrice || 0).toLocaleString('vi-VN') + ' đ',
                                    CostPriceDisplay: Math.round(x.CostPrice || 0).toLocaleString('vi-VN') + ' đ',
                                    TotalPriceDisplay: Math.round(totalPrice || 0).toLocaleString('vi-VN') + ' đ'
                                };
                            });
                            if (res.Data?.Content) res.Data.Content = mapped;
                            else if (res.Data?.Items) res.Data.Items = mapped;
                            else res.Data = mapped;
                        }
                    }
                    return res;
                })
            );
        };
    }

    ngOnInit() {
        this.menuGroupService.getByFilter({ Page: 1, Size: 1000 }).subscribe((res: any) => {
            const groups = this.getResponseItems(res) || [];
            this.menuGroupOptions.set(groups.map((group: any) => ({
                Id: group.Id,
                Name: `${group.Code} - ${group.Name}`
            })));
        });
        this.formSearch.valueChanges
            .pipe(debounceTime(600), startWith(this.formSearch.value), distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)))
            .subscribe((filters) => {
                this.onSearch(filters);
            });
    }

    override onEdit(row: any) {
        this.service.getById(row.Id).subscribe((res: any) => {
            if (res?.Code === 200 && res?.Data) {
                this.dialogService
                    .open(this.formComponent, {
                        showHeader: false,
                        data: { edition: true, data: res.Data }
                    })
                    ?.onClose.subscribe((ok) => ok && this.search({ BranchCode: this.BranchCode(), ...this.searchQuery() }));
            }
        });
    }

    checkedItems = signal<any[]>([]);
    loadingExport = signal(false);
    loadingReset = signal(false);

    onResetAvailability() {
        this.confirmationService.confirm({
            message: 'Hệ thống sẽ nạp lại số lượng tồn theo định mức ngày và bật hiển thị lại các món đang hết. Bạn có chắc chắn muốn thực hiện?',
            header: 'Xác nhận nạp lại định mức',
            icon: 'pi pi-refresh',
            acceptLabel: 'Nạp lại ngay',
            rejectLabel: 'Hủy',
            accept: () => {
                this.loadingReset.set(true);
                this.service.resetAvailability()
                    .pipe(finalize(() => this.loadingReset.set(false)))
                    .subscribe({
                        next: (res: any) => {
                            if (res?.Code === 200) {
                                this.messageService.add({
                                    severity: 'success',
                                    summary: 'Thành công',
                                    detail: res?.Message || 'Đã làm mới định mức thành công!'
                                });
                                this.search({ BranchCode: this.BranchCode(), ...this.searchQuery() });
                            } else {
                                this.messageService.add({
                                    severity: 'error',
                                    summary: 'Thất bại',
                                    detail: res?.Message || 'Nạp lại định mức thất bại!'
                                });
                            }
                        },
                        error: () => {
                            this.messageService.add({
                                severity: 'error',
                                summary: 'Lỗi',
                                detail: 'Không thể kết nối máy chủ!'
                            });
                        }
                    });
            }
        });
    }

    onCheckboxChange({ selectedRows }: any) {
        this.checkedItems.set(selectedRows ?? []);
    }

    exportExcel() {
        const ids = (this.checkedItems() || [])
            .map((x: any) => x?.Id ?? x?.id)
            .filter((x: any) => x !== null && x !== undefined && String(x).trim() !== '')
            .map((x: any) => String(x));

        const query = cleanObject({
            ...this.searchQuery(),
            BranchCode: this.BranchCode(),
            TextSearch: this.formSearch.get('TextSearch')?.value || '',
            Ids: ids.length > 0 ? ids : undefined
        });

        this.loadingExport.set(true);
        this.service
            .exportExcel(query)
            .pipe(
                finalize(() => {
                    this.loadingExport.set(false);
                })
            )
            .subscribe({
                next: (response) => {
                    const blob = response.body;
                    if (!blob || blob.size === 0) {
                        this.messageService.add({
                            severity: 'warn',
                            summary: 'Thông báo',
                            detail: 'Không có dữ liệu để tải xuống'
                        });
                        return;
                    }

                    const downloadName = this.resolveDownloadFileName(response, 'danh_sach_thuc_don', 'xlsx');
                    const url = window.URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = downloadName;
                    a.style.display = 'none';
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    window.setTimeout(() => window.URL.revokeObjectURL(url), 1000);
                },
                error: (err) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Lỗi',
                        detail: 'Xuất file Excel thất bại!'
                    });
                }
            });
    }

    handleAction({ action, row }: { action: ITableAction; row: any }) {
        if (action.type === 'edit') this.onEdit(row);
        if (action.type === 'delete') this.onDelete(row);
        if (action.type === 'toggleAvailable') {
            const nextStatus = row.IsAvailable === false;
            this.service.toggleAvailability(row.Id, nextStatus).subscribe((res: any) => {
                if (res?.Code === 200) {
                    this.messageService.add({
                        severity: 'success',
                        summary: res.Message || 'Cập nhật trạng thái hiển thị thành công!'
                    });
                    this.search({ BranchCode: this.BranchCode(), ...this.searchQuery() });
                } else {
                    this.messageService.add({
                        severity: 'error',
                        summary: res?.Message || 'Cập nhật thất bại!'
                    });
                }
            });
        }
    }
}
