import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { SliderModule } from 'primeng/slider';
import { DialogModule } from 'primeng/dialog';
import { select, Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { selectSelectedBranchCodes, selectAvailableBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { FloorPlanService } from '../../services/floor-plan';
import { DiningTableService } from '../../../dining-table/services/dining-table';
import { MessageService, ConfirmationService } from 'primeng/api';

@Component({
    selector: 'app-floor-plan',
    imports: [
        CommonModule, 
        RouterModule,
        FormsModule, 
        ReactiveFormsModule,
        ButtonModule, 
        InputTextModule, 
        InputNumberModule, 
        SelectModule, 
        SliderModule, 
        DialogModule
    ],
    templateUrl: './floor-plan.html',
    styleUrl: './floor-plan.scss'
})
export class FloorPlan implements OnInit {
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);
    private floorPlanService = inject(FloorPlanService);
    private diningTableService = inject(DiningTableService);
    private messageService = inject(MessageService);
    private confirmationService = inject(ConfirmationService);


    branchUser = signal<any[]>([]);
    selectedBranchCode = '';
    floors: any[] = [];
    tables: any[] = [];

    activeFloor: any = null;
    activeTable: any = null;
    savingLayout = false;
    zoom = signal<number>(100);

    private getZoomStorageKey(branchCode?: string, floorId?: string): string | null {
        const branch = branchCode || this.selectedBranchCode;
        const floor = floorId || this.activeFloor?.Id || this.activeFloor?.Code;
        if (!branch || !floor) return null;
        return `floor_zoom_${branch}_${floor}`;
    }

    restoreZoomForCurrentFloor() {
        const key = this.getZoomStorageKey();
        if (!key || typeof localStorage === 'undefined') {
            this.zoom.set(100);
            return;
        }
        const saved = localStorage.getItem(key);
        if (saved) {
            const val = parseInt(saved, 10);
            if (!isNaN(val) && val >= 30 && val <= 300) {
                this.zoom.set(val);
                return;
            }
        }
        this.zoom.set(100);
    }

    saveZoomForCurrentFloor(zoomValue?: number) {
        const key = this.getZoomStorageKey();
        if (!key || typeof localStorage === 'undefined') return;
        const val = zoomValue ?? this.zoom();
        localStorage.setItem(key, val.toString());
    }

    zoomIn() {
        this.zoom.update(z => {
            const newZ = Math.min(200, z + 10);
            this.saveZoomForCurrentFloor(newZ);
            return newZ;
        });
    }

    zoomOut() {
        this.zoom.update(z => {
            const newZ = Math.max(50, z - 10);
            this.saveZoomForCurrentFloor(newZ);
            return newZ;
        });
    }

    zoomReset() {
        this.zoom.set(100);
        this.saveZoomForCurrentFloor(100);
    }

    // Floor Dialog
    floorDialogVisible = false;
    floorDialogHeader = 'Thêm mặt sàn';
    floorForm: {
        Id: string;
        Code: string;
        Name: string;
        Description: string;
        IsEdit: boolean;
        Status?: number;
    } = {
        Id: '',
        Code: '',
        Name: '',
        Description: '',
        IsEdit: false,
        Status: 1
    };
    submittingFloor = false;

    // Drag-drop layout state
    private dragStartPos = { x: 0, y: 0 };
    private dragStartOffset = { x: 0, y: 0 };
    private isDragging = false;

    ngOnInit() {
        // Load branches of the user
        this.store.pipe(
            select(selectAvailableBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((branches) => {
            this.branchUser.set(branches || []);
        });

        // Sync with global branch selection
        this.store.pipe(
            select(selectSelectedBranchCodes),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((codes) => {
            if (codes && codes.length > 0) {
                const newCode = codes[0].Code || codes[0];
                if (this.selectedBranchCode !== newCode) {
                    this.selectedBranchCode = newCode;
                    this.onBranchChange();
                }
            }
        });
    }

    onBranchChange() {
        this.activeFloor = null;
        this.activeTable = null;
        this.tables = [];
        this.loadFloors();
    }

    loadFloors() {
        if (!this.selectedBranchCode) return;
        
        // Find branch Guid in branchUser options
        const branchOpt = this.branchUser().find(x => x.Code === this.selectedBranchCode);
        if (!branchOpt) return;

        this.floorPlanService.getByFilter({ BranchId: branchOpt.Id, Size: 1000, Page: 1 }).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.floors = res.Data?.Content || [];
                if (this.floors.length > 0 && !this.activeFloor) {
                    this.selectFloor(this.floors[0]);
                }
            }
        });
    }

    selectFloor(floor: any) {
        this.activeFloor = floor;
        this.activeTable = null;
        this.restoreZoomForCurrentFloor();
        this.loadTables();
    }

    loadTables() {
        if (!this.activeFloor) return;
        
        this.diningTableService.getByFilter({ FloorPlanId: this.activeFloor.Id, Size: 1000, Page: 1 }).subscribe((res: any) => {
            if (res?.Code === 200) {
                this.tables = res.Data?.Content || [];
            }
        });
    }

    // Canvas Drag-Drop
    onTableMouseDown(event: MouseEvent, table: any) {
        // Only trigger dragging for primary click
        if (event.button !== 0) return;
        
        event.preventDefault();
        event.stopPropagation();
        
        this.activeTable = table;
        this.dragStartPos = { x: event.clientX, y: event.clientY };
        this.dragStartOffset = { x: table.X, y: table.Y };
        this.isDragging = true;

        const mouseMoveHandler = (e: MouseEvent) => {
            if (!this.isDragging || !this.activeTable) return;
            const scale = this.zoom() / 100;
            const dx = (e.clientX - this.dragStartPos.x) / scale;
            const dy = (e.clientY - this.dragStartPos.y) / scale;
            
            // Constrain coords to workspace grid boundaries
            this.activeTable.X = Math.max(0, Math.round(this.dragStartOffset.x + dx));
            this.activeTable.Y = Math.max(0, Math.round(this.dragStartOffset.y + dy));
        };

        const mouseUpHandler = () => {
            this.isDragging = false;
            window.removeEventListener('mousemove', mouseMoveHandler);
            window.removeEventListener('mouseup', mouseUpHandler);
        };

        window.addEventListener('mousemove', mouseMoveHandler);
        window.addEventListener('mouseup', mouseUpHandler);
    }

    selectTable(table: any, event: MouseEvent) {
        event.stopPropagation();
        this.activeTable = table;
    }

    // Add new Table in current Floor
    addTable() {
        if (!this.activeFloor) return;
        const branchOpt = this.branchUser().find(x => x.Code === this.selectedBranchCode);
        if (!branchOpt) return;

        // Auto calculate a unique code/name
        const count = this.tables.length + 1;
        const code = `B${count.toString().padStart(2, '0')}`;
        const name = `Bàn số ${count}`;

        const newTable = {
            Id: '00000000-0000-0000-0000-000000000000', // Server creates Guid if empty/zeros
            Code: code,
            Name: name,
            Capacity: 4,
            X: 20 + (count * 10) % 200,
            Y: 20 + (count * 15) % 200,
            Width: 150,
            Height: 150,
            Rotate: 0,
            SortOrder: count
        };
        
        this.tables.push(newTable);
        this.activeTable = newTable;
    }

    removeTable(table: any) {
        this.confirmationService.confirm({
            key: 'delete-data',
            message: `Bạn có chắc chắn muốn xóa bàn ăn ${table.Name}?`,
            header: 'Xác nhận xóa',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Đồng ý',
            rejectLabel: 'Hủy',
            accept: () => {
                if (table.Id && table.Id !== '00000000-0000-0000-0000-000000000000') {
                    this.diningTableService.delete(table.Id).subscribe((res: any) => {
                        if (res?.Code === 200) {
                            this.tables = this.tables.filter(x => x.Id !== table.Id);
                            this.activeTable = null;
                            this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Bàn ăn đã được xóa!' });
                        } else {
                            this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Không thể xóa bàn ăn!' });
                        }
                    });
                } else {
                    // Local only table
                    this.tables = this.tables.filter(x => x !== table);
                    this.activeTable = null;
                }
            }
        });
    }

    saveLayout() {
        if (!this.activeFloor) return;
        this.savingLayout = true;
        
        const payload = {
            BranchCode: this.selectedBranchCode,
            FloorPlanId: this.activeFloor.Id,
            Tables: this.tables.map(x => ({
                Id: x.Id === '00000000-0000-0000-0000-000000000000' ? '00000000-0000-0000-0000-000000000000' : x.Id,
                Code: x.Code,
                Name: x.Name,
                Capacity: x.Capacity,
                X: x.X,
                Y: x.Y,
                Width: x.Width,
                Height: x.Height,
                Rotate: x.Rotate,
                SortOrder: x.SortOrder
            }))
        };

        this.diningTableService.saveLayout(payload).subscribe({
            next: (res: any) => {
                this.savingLayout = false;
                if (res?.Code === 200) {
                    this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã lưu sơ đồ bàn ăn!' });
                    this.loadTables();
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Lưu sơ đồ thất bại!' });
                }
            },
            error: () => {
                this.savingLayout = false;
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Có lỗi xảy ra khi lưu sơ đồ!' });
            }
        });
    }

    // Floor plan management dialogs
    showAddFloorDialog() {
        this.floorDialogHeader = 'Thêm mặt sàn';
        this.floorForm = {
            Id: '',
            Code: '',
            Name: '',
            Description: '',
            IsEdit: false
        };
        this.floorDialogVisible = true;
    }

    showEditFloorDialog(floor: any) {
        this.floorDialogHeader = 'Cập nhật mặt sàn';
        this.floorForm = {
            Id: floor.Id,
            Code: floor.Code,
            Name: floor.Name,
            Description: floor.Description || '',
            IsEdit: true,
            Status: floor.Status
        };
        this.floorDialogVisible = true;
    }

    saveFloor() {
        if (stringEmptyOrWhitespace(this.floorForm.Code) || stringEmptyOrWhitespace(this.floorForm.Name)) {
            this.messageService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Vui lòng điền đầy đủ mã và tên mặt sàn!' });
            return;
        }

        const branchOpt = this.branchUser().find(x => x.Code === this.selectedBranchCode);
        if (!branchOpt) return;

        this.submittingFloor = true;
        const payload = {
            Code: this.floorForm.Code,
            Name: this.floorForm.Name,
            Description: this.floorForm.Description,
            BranchId: branchOpt.Id,
            Status: (this.floorForm as any).Status || 1
        };

        if (this.floorForm.IsEdit) {
            this.floorPlanService.update({ ...payload, Id: this.floorForm.Id }).subscribe({
                next: (res: any) => {
                    this.submittingFloor = false;
                    if (res?.Code === 200) {
                        this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Cập nhật mặt sàn thành công!' });
                        this.floorDialogVisible = false;
                        this.loadFloors();
                    } else {
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Cập nhật thất bại!' });
                    }
                },
                error: () => {
                    this.submittingFloor = false;
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Đã có lỗi hệ thống xảy ra!' });
                }
            });
        } else {
            this.floorPlanService.create(payload).subscribe({
                next: (res: any) => {
                    this.submittingFloor = false;
                    if (res?.Code === 200) {
                        this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Thêm mặt sàn thành công!' });
                        this.floorDialogVisible = false;
                        this.loadFloors();
                    } else {
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Thêm mới thất bại!' });
                    }
                },
                error: () => {
                    this.submittingFloor = false;
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Đã có lỗi hệ thống xảy ra!' });
                }
            });
        }
    }

    deleteFloor(floor: any) {
        this.confirmationService.confirm({
            key: 'delete-data',
            message: `Bạn có chắc muốn xóa mặt sàn ${floor.Name}? Thao tác này cũng sẽ xóa hoặc ảnh hưởng đến các bàn ăn liên quan.`,
            header: 'Xác nhận xóa mặt sàn',
            icon: 'pi pi-trash',
            acceptLabel: 'Đồng ý',
            rejectLabel: 'Hủy',
            accept: () => {
                this.floorPlanService.delete(floor.Id).subscribe((res: any) => {
                    if (res?.Code === 200) {
                        this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Mặt sàn đã được xóa!' });
                        if (this.activeFloor?.Id === floor.Id) {
                            this.activeFloor = null;
                            this.tables = [];
                        }
                        this.loadFloors();
                    } else {
                        this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res?.Message || 'Không thể xóa mặt sàn!' });
                    }
                });
            }
        });
    }
}

function stringEmptyOrWhitespace(str: string): boolean {
    return !str || str.trim().length === 0;
}