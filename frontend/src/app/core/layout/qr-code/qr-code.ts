import { selectEmployee, selectUser } from '@/store/auth/auth.selectors';
import { selectSelectedBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { Component, computed, DestroyRef, effect, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Skeleton } from 'primeng/skeleton';
import { Select } from 'primeng/select';
import { PaymentService } from '@/core/services/payment';
import { select, Store } from '@ngrx/store';
import { PaymentRealtimeService } from '@/core/services/payment-realtime';
import { CommonModule } from '@angular/common';
import { Textarea } from "primeng/textarea";
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';

@Component({
    selector: 'app-qr-code',
    imports: [InputNumber, Skeleton, ReactiveFormsModule, ButtonModule, Select, CommonModule, Textarea],
    templateUrl: './qr-code.html',
    styleUrl: './qr-code.scss'
})
export class QrCode implements OnInit {
    transactionId = ''
    loading = false;
    qrUrl = '';
    user = signal<any | null>(null);
    employee = signal<any | null>(null);
    branchUser = signal<any[]>([]);
     dialogRef = inject(DynamicDialogRef);
     config = inject(DynamicDialogConfig, { optional: true });
    store = inject(Store);
    destroyRef: DestroyRef = inject(DestroyRef);
    messageService = inject(MessageService);
    paymentRealtime = inject(PaymentRealtimeService);
    paymentService = inject(PaymentService);

    form: FormGroup = new FormGroup({
        BranchCode: new FormControl('', Validators.required),
        AmountDue: new FormControl(0, Validators.required),
        Notes: new FormControl('')
    });

    latestStatus = computed(() => this.paymentRealtime.getLatestStatusByTransactionId(this.transactionId));

    private readonly closeOnPaymentSuccess = effect(() => {
        const status = this.latestStatus()?.status;
        if (status === 2) {
            this.dialogRef?.close({ paidSuccess: true, transactionId: this.transactionId });
             this.messageService.add({
                severity: 'success',
                summary: 'Thông báo',
                detail: 'Thanh toán thành công'
            });
        }
    });

    constructor() {
        this.store.pipe(select(selectUser), takeUntilDestroyed(this.destroyRef)).subscribe((user) => {
            this.user.set(user);
        });
        this.store.pipe(select(selectEmployee), takeUntilDestroyed(this.destroyRef)).subscribe((employee) => {
            this.employee.set(employee);
        });
        this.store.pipe(select(selectSelectedBranchCodes), takeUntilDestroyed(this.destroyRef)).subscribe((codes) => {
            if (codes?.length === 0) return;
            this.branchUser.set(codes ?? []);
            const configBranch = this.config?.data?.BranchCode;
            if (configBranch) {
                this.form.get('BranchCode')?.setValue(configBranch);
            } else {
                this.form.get('BranchCode')?.setValue(codes[0]);
            }
        });
    }
    ngOnInit(): void {
        const configData = this.config?.data;
        if (configData) {
            if (configData.AmountDue !== undefined) {
                this.form.get('AmountDue')?.setValue(configData.AmountDue);
            }
            if (configData.Notes !== undefined) {
                this.form.get('Notes')?.setValue(configData.Notes);
            }
            if (configData.TransactionId) {
                this.transactionId = configData.TransactionId;
            }
        }

        void this.initRealtime();
        
        this.form.valueChanges.subscribe(() => {
            if (!this.form.valid) {
                this.qrUrl = '';
            }
        });

        if (this.form.get('AmountDue')?.value > 0 && this.form.get('BranchCode')?.value) {
            setTimeout(() => {
                this.createQR();
            }, 300);
        }
    }
    createQR() {
        const payload = {
            TransactionId: this.transactionId,
            BranchCode: this.form.get('BranchCode')?.value,
            AmountDue: this.form.get('AmountDue')?.value,
            CustomerCode: this.user()?.Code ?? '',
            IsFastCreate: true,
            CustomerName: this.user()?.UserName ?? '',
            CreatedByUserName: this.user()?.UserName ?? this.employee()?.Name ?? '',
            CreatedByCode: this.employee()?.Code ?? this.user()?.Code ?? ''
        };

        this.paymentService.createTransaction(payload).subscribe((response: any) => {
            const code = response?.Code;
            if (code !== 200) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Thông báo',
                    detail: response?.Message ?? 'Không tạo được giao dịch thanh toán'
                });
                return;
            }
            const data: any = response?.Data ?? {};
            this.qrUrl = data?.qr_image_url ?? data?.qrImageUrl ?? data?.QRCode ?? null;
            this.messageService.add({
                severity: 'success',
                summary: 'Thông báo',
                detail: 'Tạo QR giao dịch thành công'
            });
        });
    }

    private async initRealtime(): Promise<void> {
        if (!this.transactionId) {
            this.transactionId = this.generateOrderId();
        }
        const currentOrderId = this.transactionId;
        if (!currentOrderId) return;

        try {
            await this.paymentRealtime.initializeConnection();
            await this.paymentRealtime.joinTransaction(currentOrderId);
            console.info('[PaymentDialog] Joined transaction via SignalR', currentOrderId);
        } catch (error) {
            console.error('[PaymentDialog] Failed to join transaction', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Lỗi',
                detail: 'Không thể kết nối thanh toán. Vui lòng thử lại sau.'
            });
        }
    }

    private generateOrderId(): string {
        const now = new Date();
        const pad = (value: number) => value.toString().padStart(2, '0');
        const datePart = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}`;
        const timePart = `${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
        return `PAY-${datePart}-${timePart}-${Math.floor(Math.random() * 9000 + 1000)}`;
    }
}
