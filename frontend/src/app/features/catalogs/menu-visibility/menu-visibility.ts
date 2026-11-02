import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Store } from '@ngrx/store';
import { firstValueFrom, timeout } from 'rxjs';
import { BaseHttpService } from '@/core/services/base-http';
import { selectAvailableBranchCodes, selectSelectedBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { PaginatorState } from 'primeng/paginator';
import { ITableActionEvent, ITableConfig } from '@/core/models/common';
import { TableMeta } from '@/shared/ui/data-display/table-meta/table-meta';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';

interface TimeRange { From: string; To: string }
interface Schedule {
    Kind: 'Weekdays' | 'DateRange'; Days: number[]; StartDate: string; EndDate: string;
    TimeRanges: TimeRange[]; draftFrom?: string; draftTo?: string;
}
interface Rule { Id: string; BranchCode: string; MenuItemIds: string[]; MenuGroupIds: string[]; Schedules: Schedule[]; IsEnabled: boolean }
interface Item { Id: string; Code: string; Name: string; BranchIds: string[]; Groups?: { Id: string; Name: string }[] }

@Component({
    selector: 'app-menu-visibility',
    imports: [CommonModule, FormsModule, RouterLink, ButtonModule, InputTextModule, SelectModule,
        CheckboxModule, DialogModule, MessageModule, ToastModule, IconField, InputIcon, TableMeta, DialogWrap],
    providers: [MessageService],
    templateUrl: './menu-visibility.html', styleUrl: './menu-visibility.scss'
})
export class MenuVisibility implements OnInit {
    private http = inject(BaseHttpService);
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);
    private messageService = inject(MessageService);
    branches: any[] = [];
    selectedBranchCodes: string[] = [];
    items: Item[] = [];
    rules: Rule[] = [];
    draft: Rule | null = null;
    loading = true;
    loadingItems = false;
    itemError = '';
    saving = false;
    submitted = false;
    error = '';
    success = '';
    textFilter = '';
    statusFilter = '';
    itemSearch = '';
    pendingDelete: Rule | null = null;
    page = 1;
    pageSize = 15;
    readonly statusOptions = [{ label: 'Đang bật', value: 'true' }, { label: 'Tạm ngừng', value: 'false' }];
    readonly scheduleOptions = [{ label: 'Theo thứ trong tuần', value: 'Weekdays' }, { label: 'Theo khoảng ngày', value: 'DateRange' }];
    readonly columns: ITableConfig[] = [
        { field: 'Index', label: 'STT', type: 'index', visible: true, width: '60px', align: 'center' },
        { field: 'BranchName', label: 'Chi nhánh', type: 'text', visible: true, minWidth: '160px' },
        { field: 'ItemNames', label: 'Món ăn / nhóm thực đơn', type: 'text', visible: true, minWidth: '220px' },
        { field: 'ScheduleText', label: 'Lịch ẩn (ngày / khung giờ)', type: 'text', visible: true, minWidth: '300px' },
        { field: 'IsEnabled', label: 'Trạng thái', type: 'severity', visible: true, width: '130px', severity: {
            '1': { label: 'Đang bật', severity: 'success' }, '0': { label: 'Tạm ngừng', severity: 'secondary' }
        } },
        { field: 'Action', label: 'Thao tác', type: 'action', visible: true, width: '150px', actionButtons: [
            { type: 'edit', icon: 'pi pi-file-edit', tooltipLabel: 'Chỉnh sửa' },
            { type: 'toggle', icon: row => row.IsEnabled ? 'pi pi-eye-slash' : 'pi pi-eye', tooltipLabel: row => row.IsEnabled ? 'Tắt cấu hình' : 'Bật cấu hình' },
            { type: 'delete', icon: 'pi pi-trash', tooltipLabel: 'Xóa' }
        ] }
    ];
    readonly days = [
        { value: 1, label: 'Thứ 2' }, { value: 2, label: 'Thứ 3' }, { value: 3, label: 'Thứ 4' },
        { value: 4, label: 'Thứ 5' }, { value: 5, label: 'Thứ 6' }, { value: 6, label: 'Thứ 7' }, { value: 0, label: 'Chủ nhật' }
    ];
    private filteredCache?: { rules: Rule[]; branch: string; status: string; text: string; items: Item[]; value: Rule[] };
    private tableCache?: { filtered: Rule[]; branches: any[]; items: Item[]; page: number; size: number; value: any[] };
    ngOnInit() {
        this.store.select(selectAvailableBranchCodes).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(branches => this.branches = branches || []);
        this.store.select(selectSelectedBranchCodes).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(branches => {
            this.selectedBranchCodes = (branches || []).map((branch: any) => branch.Code);
            this.page = 1;
        });
        void this.load();
    }
    private unwrap(response: any): any {
        if (response?.Code !== 200) throw new Error(response?.Message || 'Không thể xử lý yêu cầu.');
        return response.Data;
    }
    async load() {
        this.loading = true; this.error = '';
        try {
            this.rules = this.unwrap(await firstValueFrom(this.http.get('menu-visibility').pipe(timeout(10000)))) || [];
        } catch (error: any) {
            this.showError(error?.name === 'TimeoutError' ? 'Tải cấu hình quá lâu. Vui lòng thử lại.' : error?.message || 'Không tải được dữ liệu. Vui lòng thử lại.', 'Không thể tải cấu hình');
        } finally {
            this.loading = false;
            if (!this.items.length && !this.loadingItems) void this.loadItems();
        }
    }
    private async loadItems() {
        this.loadingItems = true; this.itemError = '';
        try {
            this.items = this.unwrap(await firstValueFrom(this.http.get('menu-visibility/items').pipe(timeout(10000)))) || [];
        } catch (error: any) {
            this.itemError = error?.name === 'TimeoutError' ? 'Tải danh sách món quá lâu.' : error?.message || 'Không tải được danh sách món.';
            this.showError(this.itemError, 'Không thể tải món ăn');
        } finally { this.loadingItems = false; }
    }
    retryItems() { void this.loadItems(); }
    get selectedBranches() { return this.branches.filter(branch => this.selectedBranchCodes.includes(branch.Code)); }
    branchName(code: string): string { return this.branches.find(b => b.Code === code)?.Name || code; }
    itemName(id: string): string {
        const item = this.items.find(i => i.Id === id);
        return item ? `${item.Name} (${item.Code})` : 'Món đã xóa';
    }
    get filteredRules() {
        const text = this.textFilter.trim().toLocaleLowerCase('vi');
        const selectedCodes = this.selectedBranchCodes.join(',');
        const cached = this.filteredCache;
        if (cached?.rules === this.rules && cached.branch === selectedCodes && cached.status === this.statusFilter
            && cached.text === text && cached.items === this.items) return cached.value;
        const value = this.rules.filter(r => this.selectedBranchCodes.includes(r.BranchCode)
            && (!this.statusFilter || String(r.IsEnabled) === this.statusFilter)
            && (!text || r.MenuItemIds.some(id => this.itemName(id).toLocaleLowerCase('vi').includes(text))
                || (r.MenuGroupIds || []).some(id => this.groupName(id).toLocaleLowerCase('vi').includes(text))));
        this.filteredCache = { rules: this.rules, branch: selectedCodes, status: this.statusFilter, text, items: this.items, value };
        return value;
    }
    get visibleItems() {
        const text = this.itemSearch.trim().toLocaleLowerCase('vi');
        const groupId = this.draft?.MenuGroupIds[0];
        return this.branchItems.filter(i => (!groupId || i.Groups?.some(group => group.Id === groupId))
            && (!text || `${i.Name} ${i.Code}`.toLocaleLowerCase('vi').includes(text)));
    }
    get branchItems() {
        const branch = this.branches.find(b => b.Code === this.draft?.BranchCode);
        if (!branch) return [];
        return this.items.filter(i => !i.BranchIds?.length || i.BranchIds.some(id => id.toLowerCase() === branch.Id?.toLowerCase()));
    }
    get availableGroups() {
        const groups = new Map<string, string>();
        this.branchItems.forEach(item => item.Groups?.forEach(group => groups.set(group.Id, group.Name)));
        return [...groups].map(([Id, Name]) => ({ Id, Name })).sort((a, b) => a.Name.localeCompare(b.Name, 'vi'));
    }
    groupName(id: string) {
        return this.items.flatMap(item => item.Groups || []).find(group => group.Id === id)?.Name || 'Nhóm đã xóa';
    }
    get selectedGroupId() { return this.draft?.MenuGroupIds[0] || null; }
    changeGroup(groupId: string | null) {
        if (!this.draft) return;
        this.draft.MenuGroupIds = groupId ? [groupId] : [];
        this.draft.MenuItemIds = [];
        this.itemSearch = '';
    }
    get currentPage() { return Math.min(this.page, Math.max(1, Math.ceil(this.filteredRules.length / this.pageSize))); }
    get tableRows() {
        const filtered = this.filteredRules;
        const page = this.currentPage;
        const cached = this.tableCache;
        if (cached?.filtered === filtered && cached.branches === this.branches && cached.items === this.items
            && cached.page === page && cached.size === this.pageSize) return cached.value;
        const value = filtered.slice((page - 1) * this.pageSize, page * this.pageSize).map(rule => ({
            ...rule, Source: rule, BranchName: this.branchName(rule.BranchCode),
            ItemNames: [...(rule.MenuGroupIds || []).map(id => `Nhóm: ${this.groupName(id)}`), ...rule.MenuItemIds.map(id => this.itemName(id))].join('; '),
            ScheduleText: rule.Schedules.map(s => `${this.dayText(s)}: ${this.timeText(s)}`).join('; ')
        }));
        this.tableCache = { filtered, branches: this.branches, items: this.items, page, size: this.pageSize, value };
        return value;
    }
    onPageChange(event: PaginatorState) { this.pageSize = event.rows ?? this.pageSize; this.page = (event.page ?? 0) + 1; }
    onAction(event: ITableActionEvent) {
        if (this.saving) return;
        const rule = event.row.Source as Rule;
        if (event.action.type === 'edit') this.start(rule);
        else if (event.action.type === 'toggle') void this.toggle(rule);
        else if (event.action.type === 'delete') this.pendingDelete = rule;
    }
    get allSelected() { return this.visibleItems.length > 0 && this.visibleItems.every(i => this.draft?.MenuItemIds.includes(i.Id)); }
    start(rule?: Rule) {
        this.error = ''; this.success = ''; this.itemSearch = ''; this.submitted = false;
        this.draft = rule ? structuredClone(rule) : { Id: '', BranchCode: this.selectedBranches[0]?.Code || '', MenuItemIds: [], MenuGroupIds: [], IsEnabled: true, Schedules: [] };
        this.draft.MenuGroupIds ||= [];
        if (!this.draft.Schedules.length) this.addSchedule();
    }
    back() { if (!this.saving) { this.draft = null; this.error = ''; this.submitted = false; } }
    changeBranch() { if (this.draft) { this.draft.MenuItemIds = []; this.draft.MenuGroupIds = []; } this.itemSearch = ''; }
    toggleItem(id: string, checked: boolean) {
        if (!this.draft) return;
        this.draft.MenuItemIds = checked ? [...new Set([...this.draft.MenuItemIds, id])] : this.draft.MenuItemIds.filter(x => x !== id);
    }
    toggleAll(checked: boolean) { this.visibleItems.forEach(i => this.toggleItem(i.Id, checked)); }
    addSchedule() { this.draft?.Schedules.push({ Kind: 'Weekdays', Days: [], StartDate: '', EndDate: '', TimeRanges: [] }); }
    toggleDay(schedule: Schedule, day: number, checked: boolean) { schedule.Days = checked ? [...new Set([...schedule.Days, day])] : schedule.Days.filter(d => d !== day); }
    openTimePicker(event: MouseEvent) {
        const input = event.currentTarget as HTMLInputElement;
        if (input.disabled) return;
        try { input.showPicker(); }
        catch { input.focus(); }
    }
    private showError(message: string, summary = 'Không thể lưu cấu hình') {
        this.error = message;
        this.messageService.add({ severity: 'error', summary, detail: message });
    }
    dateError(schedule: Schedule): string {
        if (!this.submitted || schedule.Kind !== 'DateRange') return '';
        if (!schedule.StartDate || !schedule.EndDate) return 'Vui lòng chọn từ ngày và đến ngày.';
        return schedule.StartDate > schedule.EndDate ? 'Đến ngày phải bằng hoặc sau từ ngày.' : '';
    }
    timeError(schedule: Schedule): string {
        if (!this.submitted || (!schedule.draftFrom && !schedule.draftTo)) return '';
        if (!schedule.draftFrom || !schedule.draftTo) return 'Chọn đủ từ giờ và đến giờ, hoặc để trống cả hai để ẩn cả ngày.';
        if (schedule.draftFrom === schedule.draftTo) return 'Giờ bắt đầu và kết thúc phải khác nhau.';
        const validTime = /^([01]\d|2[0-3]):[0-5]\d$/;
        return validTime.test(schedule.draftFrom) && validTime.test(schedule.draftTo) ? '' : 'Giờ phải đúng định dạng HH:mm.';
    }
    addTime(schedule: Schedule): boolean {
        this.submitted = true;
        const timeError = this.timeError(schedule) || (!schedule.draftFrom && !schedule.draftTo ? 'Chọn từ giờ và đến giờ trước khi thêm khung giờ.' : '');
        if (timeError) { this.showError(timeError); return false; }
        if (!schedule.draftFrom || !schedule.draftTo) return false;
        if (!schedule.TimeRanges.some(r => r.From === schedule.draftFrom && r.To === schedule.draftTo))
            schedule.TimeRanges.push({ From: schedule.draftFrom, To: schedule.draftTo });
        schedule.draftFrom = ''; schedule.draftTo = ''; this.error = ''; return true;
    }
    dayText(schedule: Schedule) {
        return schedule.Kind === 'Weekdays' ? this.days.filter(d => schedule.Days.includes(d.value)).map(d => d.label).join(', ')
            : `${this.dateText(schedule.StartDate)} – ${this.dateText(schedule.EndDate)}`;
    }
    private dateText(value: string) { return value?.split('-').reverse().join('/'); }
    timeText(schedule: Schedule) { return schedule.TimeRanges.length ? schedule.TimeRanges.map(t => `${t.From} – ${t.To}${t.From > t.To ? ' (hôm sau)' : ''}`).join(', ') : 'Cả ngày'; }
    async save() {
        if (!this.draft || this.saving || this.loadingItems || this.itemError) return;
        this.error = '';
        this.submitted = true;
        if (!this.draft.BranchCode) { this.showError('Vui lòng chọn chi nhánh.'); return; }
        if (!this.selectedBranchCodes.includes(this.draft.BranchCode)) { this.showError('Chi nhánh không còn nằm trong lựa chọn hiện tại. Vui lòng chọn lại chi nhánh.'); return; }
        if (!this.draft.MenuItemIds.length && !this.draft.MenuGroupIds.length) { this.showError('Vui lòng chọn ít nhất một món hoặc nhóm thực đơn cần ẩn.'); return; }
        if (!this.draft.Schedules.length) { this.showError('Vui lòng thêm ít nhất một lịch ẩn.'); return; }
        for (const [index, schedule] of this.draft.Schedules.entries()) {
            if (schedule.Kind === 'Weekdays' && !schedule.Days.length) { this.showError(`Lịch ẩn #${index + 1}: Vui lòng chọn ít nhất một ngày ẩn.`); return; }
            const dateError = this.dateError(schedule);
            if (dateError) { this.showError(`Lịch ẩn #${index + 1}: ${dateError}`); return; }
            const timeError = this.timeError(schedule);
            if (timeError) { this.showError(`Lịch ẩn #${index + 1}: ${timeError}`); return; }
        }
        for (const schedule of this.draft.Schedules) {
            if (schedule.draftFrom && schedule.draftTo) {
                if (!schedule.TimeRanges.some(r => r.From === schedule.draftFrom && r.To === schedule.draftTo))
                    schedule.TimeRanges.push({ From: schedule.draftFrom, To: schedule.draftTo });
                schedule.draftFrom = ''; schedule.draftTo = '';
            }
        }
        const payload = { ...this.draft, Schedules: this.draft.Schedules.map(({ draftFrom, draftTo, ...schedule }) => schedule) };
        if (!payload.Id) payload.Id = '00000000-0000-0000-0000-000000000000';
        this.saving = true;
        try {
            const request = this.draft.Id ? this.http.put('menu-visibility', payload) : this.http.post('menu-visibility', payload);
            const saved = this.unwrap(await firstValueFrom(request)) as Rule;
            this.rules = [saved, ...this.rules.filter(r => r.Id !== saved.Id)];
            this.draft = null; this.success = 'Đã lưu cấu hình hiển thị món.';
        } catch (error: any) { this.showError(error?.error?.Message || error?.message || 'Lưu thất bại. Vui lòng thử lại.'); }
        finally { this.saving = false; }
    }
    async toggle(rule: Rule) {
        if (this.saving) return;
        this.saving = true; this.error = '';
        try {
            this.unwrap(await firstValueFrom(this.http.patch(`menu-visibility/${rule.Id}/enabled`, !rule.IsEnabled)));
            this.rules = this.rules.map(r => r.Id === rule.Id ? { ...r, IsEnabled: !r.IsEnabled } : r);
        } catch (error: any) { this.showError(error?.message || 'Không thể đổi trạng thái.', 'Không thể đổi trạng thái'); }
        finally { this.saving = false; }
    }
    async remove() {
        if (!this.pendingDelete || this.saving) return;
        this.saving = true; this.error = '';
        const id = this.pendingDelete.Id;
        try {
            this.unwrap(await firstValueFrom(this.http.delete(`menu-visibility/${id}`)));
            this.rules = this.rules.filter(r => r.Id !== id); this.pendingDelete = null; this.success = 'Đã xóa cấu hình.';
        } catch (error: any) { this.showError(error?.message || 'Không thể xóa cấu hình.', 'Không thể xóa cấu hình'); this.pendingDelete = null; }
        finally { this.saving = false; }
    }
}
