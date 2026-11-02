import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { distinctUntilChanged, filter, take } from 'rxjs/operators';

import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TabsModule } from 'primeng/tabs';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { ShiftSessionService } from '../../services/shift-session';
import { WorkShiftService } from '../../../work-shift/services/work-shift';
import { BranchService } from '@/features/branch/services/branch';
import { EmployeeService } from '@/features/employee/services/empoyee';
import { selectSelectedBranchCodes, selectAvailableBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { selectUser, selectPermissions } from '@/store/auth/auth.selectors';
import AuthAction from '@/store/auth/auth.actions';

interface CashDenomination {
    denomination: number;
    quantity: number;
    amount: number;
}

import { HasPermissionDirective } from '@/shared/directive/hasPermission';

@Component({
    selector: 'app-shift-session',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        ButtonModule,
        SelectModule,
        InputNumberModule,
        InputTextModule,
        TabsModule,
        ToastModule,
        ConfirmDialogModule,
        HasPermissionDirective
    ],
    templateUrl: './shift-session.html',
    styleUrl: './shift-session.scss',
    providers: [MessageService, ConfirmationService]
})
export class ShiftSessionComponent implements OnInit {
    private fb = inject(FormBuilder);
    private router = inject(Router);
    private store = inject(Store);
    private destroyRef = inject(DestroyRef);
    private messageService = inject(MessageService);
    private confirmationService = inject(ConfirmationService);

    private shiftService = inject(ShiftSessionService);
    private workShiftService = inject(WorkShiftService);
    private branchService = inject(BranchService);
    private employeeService = inject(EmployeeService);

    // State signals
    selectedBranchCode = signal<string>('');
    selectedBranchId = signal<string>('');
    currentUser = signal<any>(null);
    currentTime = signal<Date>(new Date());
    activeShift = signal<any>(null);
    shiftSummary = signal<any>(null);
    workShifts = signal<any[]>([]);
    employees = signal<any[]>([]);
    isLoading = signal<boolean>(true); // true ngay từ đầu, không hiện form trước khi check xong
    isSubmitting = signal<boolean>(false);
    activeTabIndex = signal<number>(2);
    isCounted = signal<boolean>(false);
    lastShiftCloseTime = signal<Date | null>(null);
    lastShiftName = signal<string | null>(null);

    // Open Shift Form
    openShiftForm!: FormGroup;

    // Close Shift Form
    closeShiftForm!: FormGroup;

    // Expiry warnings
    isExpired = signal<boolean>(false);
    expiryTime = signal<Date | null>(null);
    showExtendDialog = signal<boolean>(false);
    extendMinutes = signal<number>(30);
    extendReason = signal<string>('');

    // Denominations list for cash count
    denominations = signal<CashDenomination[]>([
        { denomination: 500000, quantity: 0, amount: 0 },
        { denomination: 200000, quantity: 0, amount: 0 },
        { denomination: 100000, quantity: 0, amount: 0 },
        { denomination: 50000, quantity: 0, amount: 0 },
        { denomination: 20000, quantity: 0, amount: 0 },
        { denomination: 10000, quantity: 0, amount: 0 },
        { denomination: 5000, quantity: 0, amount: 0 },
        { denomination: 2000, quantity: 0, amount: 0 },
        { denomination: 1000, quantity: 0, amount: 0 }
    ]);

    ngOnInit() {
        this.initForms();
        this.store.pipe(
            select(selectPermissions),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((permissions) => {
            if (permissions && permissions.length > 0) {
                if (permissions.includes('SALEROOM_CASHIER_ADMIN')) {
                    this.router.navigateByUrl('/catalogs/dining-tables');
                }
            }
        });

        // 1. Get currentUser
        this.store.pipe(
            select(selectUser),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((user) => {
            if (user) {
                this.currentUser.set(user);
            }
        });

        // 2. Track selected branch code — chỉ react khi code thực sự thay đổi
        this.store.pipe(
            select(selectSelectedBranchCodes),
            filter((codes) => !!codes && codes.length > 0),
            distinctUntilChanged((a, b) => (a[0]?.Code || a[0]) === (b[0]?.Code || b[0])),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe((codes) => {
            const code = codes[0].Code || codes[0];
            this.selectedBranchCode.set(code);
            this.loadBranchIdAndInitialize(code);
        });
    }

    initForms() {
        this.openShiftForm = this.fb.group({
            shiftId: [null, Validators.required],
            openingCash: [0, [Validators.required, Validators.min(0)]]
        });

        this.closeShiftForm = this.fb.group({
            receiverEmployeeId: [null, Validators.required],
            transferCash: [0, [Validators.required, Validators.min(0)]]
        });
    }

    loadBranchIdAndInitialize(branchCode: string) {
        this.isLoading.set(true);
        this.branchService.getByFilter({ Page: 1, Size: 100 }).subscribe({
            next: (res: any) => {
                const content = res?.Data?.Content || [];
                const matchedBranch = content.find((b: any) => b.Code === branchCode);
                if (matchedBranch) {
                    this.selectedBranchId.set(matchedBranch.Id);
                    this.loadWorkShifts(matchedBranch.Id);
                    this.loadEmployees(matchedBranch.Code);
                    this.checkActiveShift(branchCode);
                } else {
                    this.isLoading.set(false);
                }
            },
            error: (err) => {
                this.isLoading.set(false);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải thông tin chi nhánh!' });
            }
        });
    }

    loadWorkShifts(branchId: string) {
        this.workShiftService.getByFilter({ BranchId: branchId, Page: 1, Size: 100 }).subscribe({
            next: (res: any) => {
                this.workShifts.set(res?.Data?.Content || []);
            }
        });
    }

    loadEmployees(branchCode: string) {
        this.employeeService.getByFilter({ Page: 1, Size: 200, IsInactive: false }).subscribe({
            next: (res: any) => {
                const content = res?.Data?.Content || [];
                // Filter employees applicable to this branch
                this.employees.set(content);
            }
        });
    }

    checkActiveShift(branchCode: string) {
        this.shiftService.getCurrentShift(branchCode).subscribe({
            next: (res: any) => {
                if (res.Code === 200 && res.Data) {
                    this.activeShift.set(res.Data);
                    this.isExpired.set(res.Data.IsExpired);
                    if (res.Data.ExpiryTime) {
                        this.expiryTime.set(new Date(res.Data.ExpiryTime));
                    }
                    this.isCounted.set(res.Data.ActualCash > 0 || res.Data.CashCounts?.length > 0);

                    this.isLoading.set(false);

                    // Pre-fill cash counts if already saved
                    if (res.Data.CashCounts && res.Data.CashCounts.length > 0) {
                        const savedCounts = res.Data.CashCounts;
                        const currentDenoms = this.denominations().map(d => {
                            const matched = savedCounts.find((sc: any) => sc.Denomination === d.denomination);
                            if (matched) {
                                return {
                                    ...d,
                                    quantity: matched.Quantity,
                                    amount: d.denomination * matched.Quantity
                                };
                            }
                            return d;
                        });
                        this.denominations.set(currentDenoms);
                    }

                    this.loadShiftSummary(res.Data.Id);
                } else {
                    // Không có ca → hiện form mở ca, mới set loading=false
                    this.isLoading.set(false);
                    this.activeShift.set(null);
                    this.shiftSummary.set(null);
                    this.currentTime.set(new Date());
                    const defaultOpeningCash = res.Data?.DefaultOpeningCash || 0;
                    if (res.Data?.LastShiftCloseTime) {
                        this.lastShiftCloseTime.set(new Date(res.Data.LastShiftCloseTime));
                    } else {
                        this.lastShiftCloseTime.set(null);
                    }
                    this.lastShiftName.set(res.Data?.LastShiftName || null);

                    this.openShiftForm.patchValue({
                        openingCash: defaultOpeningCash
                    });
                }
            },
            error: (err) => {
                this.isLoading.set(false);
                this.activeShift.set(null);
            }
        });
    }

    loadShiftSummary(shiftId: string) {
        this.shiftService.getShiftSummary(shiftId).subscribe({
            next: (res: any) => {
                if (res.Code === 200) {
                    this.shiftSummary.set(res.Data);

                    // Quy tắc: Số tiền bàn giao thực tế = Tiền mặt kiểm kê (nếu chưa kiểm kê thì là 0)
                    const totalActualCount = this.getTotalActualCash();
                    const actualCash = (res.Data.ActualCash && res.Data.ActualCash > 0)
                        ? res.Data.ActualCash
                        : (totalActualCount > 0 ? totalActualCount : 0);

                    this.closeShiftForm.patchValue({
                        transferCash: actualCash
                    });
                }
            }
        });
    }

    onOpenShift() {
        if (this.openShiftForm.invalid) {
            this.openShiftForm.markAllAsTouched();
            return;
        }

        this.isSubmitting.set(true);
        const data = {
            ...this.openShiftForm.value,
            branchCode: this.selectedBranchCode(),
            machineCode: 'POS-01' // default machine code
        };

        this.shiftService.openShift(data).subscribe({
            next: (res: any) => {
                this.isSubmitting.set(false);
                if (res.Code === 200) {
                    this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Mở ca làm việc thành công!' });
                    this.router.navigateByUrl('/catalogs/dining-tables');
                } else if (res.Code === 409) {
                    this.confirmationService.confirm({
                        message: res.Message || 'Bạn đang có một ca làm việc đang mở. Bạn có muốn chuyển sang màn hình quản lý ca làm việc hiện tại không?',
                        header: 'Trùng ca làm việc',
                        icon: 'pi pi-exclamation-triangle',
                        acceptButtonProps: { label: 'Đồng ý', severity: 'primary' },
                        rejectButtonProps: { label: 'Hủy bỏ', severity: 'secondary', outlined: true },
                        accept: () => {
                            this.checkActiveShift(this.selectedBranchCode());
                        }
                    });
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
                }
            },
            error: (err) => {
                this.isSubmitting.set(false);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err.error?.Message || 'Có lỗi xảy ra khi mở ca!' });
            }
        });
    }

    onQuantityChange(index: number, quantity: number) {
        const list = [...this.denominations()];
        const item = { ...list[index] };
        item.quantity = quantity || 0;
        item.amount = item.denomination * item.quantity;
        list[index] = item;
        this.denominations.set(list);

        // Quy tắc: Số tiền bàn giao thực tế = Tiền mặt kiểm kê (luôn sync, kể cả khi = 0)
        const totalActual = this.getTotalActualCash();
        this.closeShiftForm.patchValue({
            transferCash: totalActual
        });
    }

    getTotalActualCash(): number {
        return this.denominations().reduce((sum, item) => sum + item.amount, 0);
    }

    onSubmitCashCount() {
        if (!this.activeShift()) return;

        this.isSubmitting.set(true);
        const items = this.denominations().map(d => ({
            denomination: d.denomination,
            quantity: d.quantity
        }));

        this.shiftService.submitCashCount(this.activeShift().Id, items).subscribe({
            next: (res: any) => {
                this.isSubmitting.set(false);
                if (res.Code === 200) {
                    this.isCounted.set(true);
                    this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Kiểm kê tiền mặt thành công!' });

                    // Luôn đồng bộ: Số tiền bàn giao thực tế = Tiền mặt kiểm kê (dù = 0)
                    const totalActual = this.getTotalActualCash();
                    this.closeShiftForm.patchValue({
                        transferCash: totalActual
                    });
                    this.loadShiftSummary(this.activeShift().Id);
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
                }
            },
            error: (err) => {
                this.isSubmitting.set(false);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Lỗi kiểm kê!' });
            }
        });
    }

    onCloseShift() {
        if (this.closeShiftForm.invalid) {
            this.closeShiftForm.markAllAsTouched();
            return;
        }

        // Nếu chưa kiểm kê tiền mặt → hỏi confirm, không block cứng
        if (!this.isCounted()) {
            this.confirmationService.confirm({
                message: 'Bạn chưa kiểm kê tiền mặt. Số tiền bàn giao sẽ là số nhập thủ công. Bạn có chắc muốn tiếp tục đóng ca không?',
                header: 'Chưa kiểm kê tiền mặt',
                icon: 'pi pi-exclamation-triangle',
                acceptButtonProps: { label: 'Vẫn đóng ca', severity: 'warning' },
                rejectButtonProps: { label: 'Quay lại kiểm kê', severity: 'secondary', outlined: true },
                accept: () => this.doCloseShift()
            });
            return;
        }

        this.doCloseShift();
    }

    private doCloseShift() {
        this.isSubmitting.set(true);
        const data = {
            shiftId: this.activeShift().Id,
            ...this.closeShiftForm.value
        };

        this.shiftService.closeShift(data).subscribe({
            next: (res: any) => {
                this.isSubmitting.set(false);
                if (res.Code === 200) {
                    this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Đóng ca thành công! Đang đăng xuất...' });
                    setTimeout(() => {
                        this.store.dispatch(AuthAction.logout());
                    }, 1500);
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
                }
            },
            error: (err) => {
                this.isSubmitting.set(false);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: err.error?.Message || 'Có lỗi xảy ra!' });
            }
        });
    }

    onExtendShift() {
        if (this.extendMinutes() <= 0) return;

        this.isSubmitting.set(true);
        const data = {
            shiftId: this.activeShift().Id,
            extendedMinutes: this.extendMinutes(),
            reason: this.extendReason()
        };

        this.shiftService.extendShift(data).subscribe({
            next: (res: any) => {
                this.isSubmitting.set(false);
                if (res.Code === 200) {
                    this.showExtendDialog.set(false);
                    this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Gia hạn ca thành công!' });
                    this.checkActiveShift(this.selectedBranchCode());
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: res.Message });
                }
            },
            error: (err) => {
                this.isSubmitting.set(false);
                this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể gia hạn!' });
            }
        });
    }

    onTabChange(index: number) {
        this.activeTabIndex.set(index);
        if (index === 1) {
            // Số tiền bàn giao thực tế = Tiền mặt kiểm kê (tổng tiền bên tab Kiểm kê tiền mặt)
            const actualCash = this.getActualCashToShow();
            this.closeShiftForm.patchValue({
                transferCash: actualCash
            });
        }
    }

    getActualCashToShow(): number {
        // Luôn trả về Tiền mặt kiểm kê (tổng tiền bên tab Kiểm kê tiền mặt)
        const totalActualCount = this.getTotalActualCash();
        if (totalActualCount > 0) {
            return totalActualCount;
        }
        return this.shiftSummary()?.ActualCash || 0;
    }

    getDifferenceCashToShow(): number {
        // Tiền chênh lệch = Tiền mặt phải có - tiền mặt kiểm kê
        const expected = this.shiftSummary()?.ExpectedCash || 0;
        const actual = this.getActualCashToShow();
        return expected - actual;
    }

    navigateToPos() {
        this.router.navigateByUrl('/catalogs/dining-tables');
    }

    onCancel() {
        this.router.navigateByUrl('/catalogs/dining-tables');
    }
}
