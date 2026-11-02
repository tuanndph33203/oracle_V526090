import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageService } from 'primeng/api';
import { Store, select } from '@ngrx/store';
import { selectSystemSettings } from '@/store/system-settings/system-settings.selectors';
import { selectUser, selectEmployee } from '@/store/auth/auth.selectors';
import { OrderCalculator } from '@/core/utils/order-calculator';
import { environment } from 'src/environments/environment';
import { PaymentService } from '@/core/services/payment';
import { SalesService } from '../../../dining-table/services/sales';
import { PromotionService } from '@/features/catalogs/promotions/services/promotion';

@Component({
    selector: 'app-order-details-dialog',
    imports: [CommonModule, ButtonModule, DialogModule],
    templateUrl: './order-details-dialog.html'
})
export class OrderDetailsDialog implements OnInit {
    public ref = inject(DynamicDialogRef);
    public config = inject(DynamicDialogConfig);
    private store = inject(Store);
    private paymentService = inject(PaymentService);
    private salesService = inject(SalesService);
    private promotionService = inject(PromotionService);
    private messageService = inject(MessageService);

    order = this.config.data?.order;
    private optionGroupCache = new WeakMap<object, { name: string; values: string[]; extraCharge: number }[]>();
    branchCode = this.config.data?.branchCode || this.order?.BranchCode || '';
    branchName = this.config.data?.branchName || this.order?.BranchName || '';
    requireKitchenSend = signal<boolean>(true);
    eInvoiceBranchConfigs = signal<any[]>([]);
    promotionsList = signal<any[]>([]);
    currentUser = signal<any>(null);
    currentEmployee = signal<any>(null);
    eInvoiceDialogVisible = false;
    isPrinting = signal<boolean>(false);

    ngOnInit() {
        this.store.pipe(select(selectUser)).subscribe((user) => {
            this.currentUser.set(user);
        });

        this.store.pipe(select(selectEmployee)).subscribe((emp) => {
            this.currentEmployee.set(emp);
        });

        this.store.pipe(select(selectSystemSettings)).subscribe((configs) => {
            const setting = configs.find(x => `${x.Code ?? ''}`.trim().toLowerCase() === 'sale-require-kitchen');
            if (setting) {
                const val = `${setting.Value ?? ''}`.trim().toLowerCase();
                const isTrue = val === 'true' || val === '1' || val === 'yes' || val === 'on';
                this.requireKitchenSend.set(isTrue && setting.IsActive);
            } else {
                this.requireKitchenSend.set(true);
            }
        });

        this.promotionService.getByFilter({ Page: 1, Size: 1000 }).subscribe({
            next: (res: any) => {
                if (res?.Code === 200 && Array.isArray(res?.Data?.Content)) {
                    this.promotionsList.set(res.Data.Content);
                }
            }
        });
    }

    getStatusBadgeClass(status: string): string {
        if (!status) return 'bg-surface-100 text-surface-700 dark:bg-surface-800 dark:text-surface-300';
        const s = status.toUpperCase();
        switch (s) {
            case 'SERVING':
                return 'bg-primary/10 text-primary border border-primary/20 font-bold';
            case 'CHECKED':
                return 'bg-blue-100 text-blue-800 dark:bg-blue-950/60 dark:text-blue-300 border border-blue-300 dark:border-blue-800 font-bold';
            case 'WAITINGPAYMENT':
            case 'WAITINGCONFIRM':
                return 'bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-300 border border-amber-300 dark:border-amber-800 font-bold';
            case 'RESERVED':
                return 'bg-purple-100 text-purple-800 dark:bg-purple-950/60 dark:text-purple-300 border border-purple-300 dark:border-purple-800 font-bold';
            case 'PAID':
            case 'COMPLETED':
                return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800 font-bold';
            case 'CANCELLED':
            case 'APPROVEDCANCELLED':
                return 'bg-red-100 text-red-800 dark:bg-red-950/60 dark:text-red-300 border border-red-300 dark:border-red-800 font-bold';
            case 'MERGED':
                return 'bg-surface-200 text-surface-800 dark:bg-surface-700 dark:text-surface-200 font-bold';
            default:
                return 'bg-surface-100 text-surface-700 dark:bg-surface-800 dark:text-surface-300';
        }
    }

    getStatusText(status: string): string {
        if (!status) return '-';
        const s = status.toUpperCase();
        switch (s) {
            case 'SERVING':
                return 'Đang phục vụ';
            case 'RESERVED':
                return 'Đặt trước';
            case 'PAID':
            case 'COMPLETED':
                return 'Đã thanh toán';
            case 'CANCELLED':
                return 'Đã hủy';
            case 'APPROVEDCANCELLED':
                return 'Đã duyệt hủy';
            case 'MERGED':
                return 'Đã gộp';
            case 'WAITINGPAYMENT':
                return 'Chờ thanh toán';
            case 'WAITINGCONFIRM':
                return 'Chờ xác nhận';
            case 'CHECKED':
                return 'Đã kiểm đồ';
            case 'DRAFT':
                return 'Nháp';
            default:
                return status;
        }
    }

    getPaymentTypeName(pay: any): string {
        if (!pay) return '';
        const type = pay.PaymentType || '';
        if (type === 'Cash') return 'Tiền mặt';
        if (type === 'Transfer' || type === 'QR' || type === 'Bank') return 'Chuyển khoản';
        if (type === 'CreditCard' || type === 'Card') return 'Thẻ tín dụng';
        if (type === 'Tiền khuyến mãi' || type === 'Discount' || type === 'DiscountAmount' || type === 'Promotion' || type === 'Voucher' || type === 'Tiền chiết khấu' || (pay.PaymentAmount && pay.PaymentAmount < 0)) {
            return 'Tiền chiết khấu';
        }
        return type;
    }

    isItemCancelled(item: any): boolean {
        if (!item) return false;
        if (item.IsCancelled === 1 || item.isCancelled === 1 || item.KitchenStatus === 'Cancelled') return true;
        const totalInvalid = (item.SoldOutQty || 0) + (item.CancelledQuantity || 0) + (item.ReturnedQuantity || 0);
        return item.Quantity > 0 && totalInvalid >= item.Quantity;
    }

    getSelectedOptionGroups(item: any): { name: string; values: string[]; extraCharge: number }[] {
        if (!item || typeof item !== 'object') return [];
        const cached = this.optionGroupCache.get(item);
        if (cached) return cached;
        let options = Array.isArray(item.SelectedOptions) ? item.SelectedOptions : [];
        if (!options.length && item.SelectedOptionsJson) {
            try {
                const parsed = JSON.parse(item.SelectedOptionsJson);
                options = Array.isArray(parsed) ? parsed : [];
            } catch { options = []; }
        }
        const groups = new Map<string, { name: string; values: string[]; extraCharge: number }>();
        for (const option of options) {
            const name = String(option.GroupName || option.GroupCode || 'Tùy chọn');
            const key = String(option.GroupCode || name).toLowerCase();
            const group = groups.get(key) || { name, values: [], extraCharge: 0 };
            group.values.push(String(option.ValueName || option.ValueCode || ''));
            group.extraCharge += Number(option.ExtraCharge || 0);
            groups.set(key, group);
        }
        const result = Array.from(groups.values());
        this.optionGroupCache.set(item, result);
        return result;
    }

    getBaseUnitPrice(item: any): number {
        const unitPrice = Number(item?.UnitPrice ?? item?.Price ?? 0);
        const optionPrice = this.getSelectedOptionGroups(item)
            .reduce((sum, group) => sum + group.extraCharge, 0);
        return unitPrice - optionPrice;
    }


    getItemTotalAmount(item: any): number {
        if (!item) return 0;
        if (this.isItemCancelled(item)) return 0;
        return item.TotalAmount ?? 0;
    }

    parseShippingFee(val: any): number {
        if (!val) return 0;
        if (typeof val === 'number') return val;
        let str = String(val).trim().toLowerCase();
        if (!str) return 0;
        str = str.replace(/([0-9]+(?:\.[0-9]+)?)\s*k\b/g, (_, p1) => String(parseFloat(p1) * 1000));
        const match = str.match(/\d[\d\.\,]*/);
        if (match) {
            const cleanNum = match[0].replace(/[\.\,]/g, '');
            const num = parseFloat(cleanNum);
            return isNaN(num) ? 0 : num;
        }
        return 0;
    }

    getShippingFee(): number {
        if (this.order?.ServiceType === 'Delivery') {
            return this.parseShippingFee(this.order.ShippingFeeNote);
        }
        return 0;
    }

    getOrderTotalAmount(): number {
        if (!this.order) return 0;
        return (this.order.TotalAmount || 0) + this.getShippingFee();
    }

    isCurrentBranchEInvoiceApplied(): boolean {
        return false;
    }

    openEInvoicePrompt() {
        if (!this.order) return;
        this.executePrintTemporaryBill(false);
    }

    closeEInvoicePrompt() {
        this.eInvoiceDialogVisible = false;
    }

    executePrintTemporaryBill(showInvoiceQr: boolean) {
        this.eInvoiceDialogVisible = false;
        this.startPrintProcess(showInvoiceQr);
    }

    startPrintProcess(showInvoiceQr: boolean) {
        if (!this.order) return;
        const order = this.order;
        const orderId = order.OrderId || order.Id || '';
        const branchCode = this.branchCode || order.BranchCode || '';
        const totalAmt = this.getOrderTotalAmount();
        const transactionId = order.BillCode || order.OrderCode || `PAY-${orderId}-${Date.now()}`;

        this.isPrinting.set(true);

        const qrPayload = {
            TransactionId: transactionId,
            BranchCode: branchCode,
            AmountDue: totalAmt,
            CustomerCode: order.CustomerCode || '',
            IsFastCreate: false,
            CustomerName: order.CustomerName || '',
            CreatedByUserName: '',
            CreatedByCode: ''
        };

        // 1. Fetch Payment QR (VietQR)
        this.paymentService.createTransaction(qrPayload).subscribe({
            next: (qrRes: any) => {
                const qrUrl = qrRes?.Data?.qr_image_url ?? qrRes?.Data?.qrImageUrl ?? qrRes?.Data?.QRCode ?? null;
                this.resolveInvoiceUrlAndPrint(order, orderId, branchCode, qrUrl, showInvoiceQr);
            },
            error: () => {
                this.resolveInvoiceUrlAndPrint(order, orderId, branchCode, null, showInvoiceQr);
            }
        });
    }

    private resolveInvoiceUrlAndPrint(order: any, orderId: string, branchCode: string, qrUrl: string | null, showInvoiceQr: boolean) {
        if (!showInvoiceQr) {
            this.isPrinting.set(false);
            this.printTemporaryBill(qrUrl, null, false);
            return;
        }

        if (branchCode && orderId) {
            this.salesService.printTemporaryBill(orderId, branchCode).subscribe({
                next: (res: any) => {
                    this.isPrinting.set(false);
                    const invoiceRegisterUrl = (res?.Code === 200 && res?.Data) ? res.Data : null;
                    this.printTemporaryBill(qrUrl, invoiceRegisterUrl, true);
                },
                error: () => {
                    this.isPrinting.set(false);
                    this.printTemporaryBill(qrUrl, null, true);
                }
            });
        } else {
            this.isPrinting.set(false);
            this.printTemporaryBill(qrUrl, null, true);
        }
    }

    printHtmlViaHiddenIframe(htmlContent: string) {
        let iframe = document.getElementById('pos-silent-print-frame') as HTMLIFrameElement;
        if (!iframe) {
            iframe = document.createElement('iframe');
            iframe.id = 'pos-silent-print-frame';
            iframe.style.position = 'fixed';
            iframe.style.right = '0';
            iframe.style.bottom = '0';
            iframe.style.width = '0';
            iframe.style.height = '0';
            iframe.style.border = '0';
            iframe.style.visibility = 'hidden';
            iframe.style.zIndex = '-9999';
            document.body.appendChild(iframe);
        }

        const iframeWin = iframe.contentWindow;
        const iframeDoc = iframeWin?.document || iframe.contentDocument;
        if (!iframeDoc || !iframeWin) return;

        iframeDoc.open();
        iframeDoc.write(htmlContent);
        iframeDoc.close();

        let printed = false;
        const triggerPrint = () => {
            if (printed) return;
            printed = true;
            try {
                iframeWin.focus();
                iframeWin.print();
            } catch (err) {
                console.error('Lỗi khi in qua iframe:', err);
            }
        };

        const imgs = iframeDoc.images;
        if (imgs && imgs.length > 0) {
            let loaded = 0;
            const total = imgs.length;
            const checkDone = () => {
                loaded++;
                if (loaded >= total) setTimeout(triggerPrint, 100);
            };
            for (let i = 0; i < total; i++) {
                if (imgs[i].complete) {
                    loaded++;
                } else {
                    imgs[i].onload = checkDone;
                    imgs[i].onerror = checkDone;
                }
            }
            if (loaded >= total) {
                setTimeout(triggerPrint, 250);
            } else {
                setTimeout(triggerPrint, 1500);
            }
        } else {
            iframeWin.onload = triggerPrint;
            setTimeout(triggerPrint, 350);
        }
    }

    printTemporaryBill(qrUrl?: string | null, invoiceRegisterUrl?: string | null, showInvoiceQr: boolean = true) {
        if (!this.order) return;
        const order = this.order;

        const dateStr = new Date().toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' });
        const checkInTime = order.OpenTime ? new Date(order.OpenTime).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : dateStr;
        const loggedInName = this.currentEmployee()?.Name 
            || this.currentEmployee()?.FullName 
            || this.currentUser()?.FullName 
            || this.currentUser()?.fullName 
            || this.currentUser()?.Name 
            || this.currentUser()?.name 
            || this.currentUser()?.UserName 
            || this.currentUser()?.userName 
            || (this.currentUser()?.email ? this.currentUser().email.split('@')[0] : '');

        const waiter = loggedInName 
            || order.CashierUserName 
            || order.CashierName 
            || order.WaiterName 
            || order.CreatedByUserName 
            || 'N/A';
        const items = order.Items || [];

        // Vietnamese Number to Words converter
        const docSoTien = (soTien: number): string => {
            if (soTien === 0) return 'Không đồng';
            const ChuSo = ["không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"];
            const Tien = ["", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ"];

            function DocBlock(so: number, day: boolean): string {
                let chuoi = "";
                const tram = Math.floor(so / 100);
                const chuc = Math.floor((so % 100) / 10);
                const donvi = so % 10;

                if (tram > 0 || day) {
                    chuoi += ChuSo[tram] + " trăm ";
                }

                if (chuc > 0) {
                    if (chuc === 1) chuoi += "mười ";
                    else chuoi += ChuSo[chuc] + " mươi ";
                } else if (donvi > 0 && (tram > 0 || day)) {
                    chuoi += "lẻ ";
                }

                if (donvi > 0) {
                    if (donvi === 1 && chuc > 1) chuoi += "mốt";
                    else if (donvi === 5 && chuc > 0) chuoi += "lăm";
                    else chuoi += ChuSo[donvi];
                }
                return chuoi;
            }

            let viTri = 0;
            let chuoi = "";
            let temp = Math.round(soTien);

            if (temp < 0) return "Trừ " + docSoTien(-temp);

            const blocks: number[] = [];
            while (temp > 0) {
                blocks.push(temp % 1000);
                temp = Math.floor(temp / 1000);
            }

            for (let i = blocks.length - 1; i >= 0; i--) {
                const block = blocks[i];
                if (block > 0) {
                    const hienThi = DocBlock(block, i < blocks.length - 1);
                    chuoi += hienThi + " " + Tien[i] + " ";
                }
            }

            chuoi = chuoi.trim();
            if (chuoi.length > 0) {
                chuoi = chuoi.substring(0, 1).toUpperCase() + chuoi.substring(1) + " đồng chẵn.";
            }
            return chuoi.replace(/\s+/g, ' ');
        };

        const getActualQty = (item: any) => {
            if (!item) return 0;
            if (item.IsCancelled === 1) return 0;
            const totalInvalid = (item.SoldOutQty || 0) + (item.CancelledQuantity || 0) + (item.ReturnedQuantity || 0);
            return Math.max(0, (item.Quantity || 0) - totalInvalid);
        };

        const calcResult = OrderCalculator.calculateTaxGroups(items, item => getActualQty(item));
        const taxGroups = calcResult.taxGroups;
        const isDeliveryOrder = order.ServiceType === 'Delivery';
        const shippingFeeAmt = isDeliveryOrder ? this.getShippingFee() : 0;
        const total = calcResult.total + shippingFeeAmt;

        const group8 = taxGroups['8%'] || { rate: 8, subtotal: 0, vatAmt: 0, totalAmt: 0 };
        const group10 = taxGroups['10%'] || { rate: 10, subtotal: 0, vatAmt: 0, totalAmt: 0 };

        let otherSubtotal = 0;
        let otherVatAmt = 0;
        let otherTotalAmt = 0;
        Object.keys(taxGroups).forEach(k => {
            if (k !== '8%' && k !== '10%') {
                otherSubtotal += taxGroups[k].subtotal;
                otherVatAmt += taxGroups[k].vatAmt;
                otherTotalAmt += taxGroups[k].totalAmt;
            }
        });

        let itemsHtml = '';
        let lineIdx = 0;
        items.forEach((item: any) => {
            const qty = getActualQty(item);
            if (qty <= 0) return;
            lineIdx++;
            const unitPrice = item.Price !== undefined ? item.Price : (item.UnitPrice !== undefined ? item.UnitPrice : 0);
            const fixedTax = (item.FixedTaxAmount !== undefined && item.FixedTaxAmount !== null && Number(item.FixedTaxAmount) > 0)
                ? Number(item.FixedTaxAmount)
                : (item.fixedTaxAmount !== undefined && item.fixedTaxAmount !== null && Number(item.fixedTaxAmount) > 0 ? Number(item.fixedTaxAmount) : 0);
            const vatRate = Number(item.VatRate || 0);

            let unitTax = 0;
            if (fixedTax > 0) {
                unitTax = Math.round(fixedTax);
            } else if (vatRate > 0) {
                unitTax = Math.round(unitPrice * (vatRate / 100));
            }
            const displayUnitPrice = unitPrice + unitTax;

            let unitDiscount = 0;
            let discountPercent = 0;
            if (item.DiscountRate !== undefined && item.DiscountRate !== null && Number(item.DiscountRate) > 0) {
                discountPercent = Number(item.DiscountRate);
                unitDiscount = Math.round(displayUnitPrice * (discountPercent / 100));
            } else if (item.DiscountAmount !== undefined && item.DiscountAmount !== null && item.DiscountAmount > 0 && qty > 0) {
                unitDiscount = Math.round(item.DiscountAmount / qty);
                if (displayUnitPrice > 0) {
                    discountPercent = Math.round((unitDiscount / displayUnitPrice) * 100);
                }
            }

            const unitTotal = Math.max(0, displayUnitPrice - unitDiscount);
            const lineTotal = Math.round(unitTotal * qty);
            const discountPercentStr = discountPercent > 0 ? `${discountPercent}%` : '0%';

            const itemNote = item.Note || item.note || item.ItemNote || item.NoteText || item.Notes || '';
            itemsHtml += `
                <tr>
                    <td style="text-align: left; padding: 3px 0; vertical-align: top;">${lineIdx}</td>
                    <td style="text-align: left; padding: 3px 0; font-weight: 500;">
                        ${item.ItemName || item.MenuItemName}
                        ${itemNote ? `<br><span style="font-size: 10px; font-weight: 700; color: #000;">* Ghi chú: ${itemNote}</span>` : ''}
                    </td>
                    <td style="text-align: center; padding: 3px 0; vertical-align: top;">${qty}</td>
                    <td style="text-align: right; padding: 3px 0; vertical-align: top;">${displayUnitPrice.toLocaleString('vi-VN')}</td>
                    <td style="text-align: right; padding: 3px 0; vertical-align: top;">${discountPercentStr}</td>
                    <td style="text-align: right; padding: 3px 0; vertical-align: top; font-weight: 500;">
                        ${lineTotal.toLocaleString('vi-VN')}
                    </td>
                </tr>
            `;
        });

        // Collect applied promotions
        let appliedPromoNames: string[] = [];
        const promoCode = order.SelectedPromotionCode;
        if (promoCode) {
            const promo = (this.promotionsList() || []).find((p: any) => p.Code === promoCode);
            const pName = promo ? (promo.Name || promo.Description) : null;
            if (pName) {
                appliedPromoNames.push(pName);
            } else if (order.DiscountLabel) {
                appliedPromoNames.push(order.DiscountLabel);
            } else {
                appliedPromoNames.push(`Khuyến mại: ${promoCode}`);
            }
        } else if (order.DiscountLabel) {
            appliedPromoNames.push(order.DiscountLabel);
        }
        (order.Items || []).forEach((item: any) => {
            const itemPromoCode = item.PromotionCode || item.promotionCode;
            if (itemPromoCode) {
                const promo = (this.promotionsList() || []).find((p: any) => p.Code === itemPromoCode);
                const pName = promo ? (promo.Name || promo.Description) : null;
                if (pName && !appliedPromoNames.includes(pName)) {
                    appliedPromoNames.push(pName);
                } else if (!pName && !appliedPromoNames.some(x => x.includes(itemPromoCode))) {
                    appliedPromoNames.push(`Khuyến mại: ${itemPromoCode}`);
                }
            }
        });

        const mainOrderNote = order.Note || order.OrderNote || '';
        const formattedTotalWords = docSoTien(total);
        const tableName = order.TableName
            || order.TableNames
            || (order.Tables && order.Tables.length > 0 ? order.Tables.map((t: any) => t.TableName || t.Name || t.TableCode).join(' + ') : '')
            || order.TableInfo
            || (order.ServiceType === 'TakeAway' ? 'Mang về' : (order.ServiceType === 'Delivery' ? 'Giao hàng' : 'N/A'));

        // Customer registration URL for e-invoice
        let landingUrl = invoiceRegisterUrl || order.InvoiceRegisterUrl;
        if (!landingUrl) {
            landingUrl = environment.eInvoiceLandingUrl || '/register-invoice/';
            if (landingUrl.startsWith('/')) {
                landingUrl = `${window.location.protocol}//${window.location.host}${landingUrl}`;
            }
            landingUrl = `${landingUrl}${order.OrderId || order.Id || ''}`;
        }
        const invoiceQrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=100x100&data=${encodeURIComponent(landingUrl)}`;
        const storeTitle = 'UTC COFFEE';
        const billTitle = (order.Status === 'Paid' || order.Status === 'Completed') ? 'HÓA ĐƠN THANH TOÁN' : 'PHIẾU TẠM TÍNH';

        const contentHtml = `
            <html>
            <head>
              <title>${billTitle}</title>
              <link rel="preconnect" href="https://fonts.googleapis.com">
              <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
              <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">
              <style>
                @page {
                  size: auto;
                  margin: 0mm;
                }
                html, body {
                  margin: 0;
                  padding: 0;
                  width: 100%;
                }
                body {
                  font-family: 'Inter', sans-serif;
                  font-size: 9.5px;
                  line-height: 1.35;
                  padding: 0 4px 4px 4px;
                  box-sizing: border-box;
                  color: #000;
                }
                .text-center { text-align: center; }
                .text-right { text-align: right; }
                .bold { font-weight: 700; }
                .divider { border-top: 1px dashed #000; margin: 5px 0; }
                .double-divider { border-top: 1px solid #000; border-bottom: 1px solid #000; height: 2px; margin: 5px 0; }

                .store-title { font-size: 11px; font-weight: 700; text-transform: uppercase; margin-top: 0; margin-bottom: 2px; }
                .bill-title { font-size: 13px; font-weight: 800; text-transform: uppercase; margin: 4px 0; letter-spacing: 0.5px; }

                .info-table { width: 100%; border-collapse: collapse; margin-bottom: 4px; font-size: 9px; }
                .info-table td { padding: 1px 0; }

                .items-table { width: 100%; border-collapse: collapse; margin: 4px 0; font-size: 9px; }
                .items-table th { font-weight: 700; border-bottom: 1px dashed #000; padding: 4px 0; text-align: left; }
                .items-table td { padding: 3px 0; }

                .summary-table { width: 100%; border-collapse: collapse; font-size: 10px; margin-top: 4px; }
                .summary-table th { font-weight: 700; padding: 2px 3px; text-align: right; white-space: nowrap; }
                .summary-table td { padding: 2px 3px; text-align: right; white-space: nowrap; }

                .total-box { display: flex; justify-content: space-between; font-weight: 800; font-size: 12px; margin: 5px 0 2px 0; }
                .words-text { font-size: 10px; font-style: italic; margin-bottom: 4px; line-height: 1.2; }

                .qr-section { margin: 8px 0; display: flex; align-items: center; gap: 8px; page-break-inside: avoid; }
                .qr-copy { flex: 1; min-width: 0; text-align: left; }
                .qr-title { font-size: 11px; font-weight: 800; text-transform: uppercase; margin-bottom: 4px; color: #000; }
                .qr-sub { font-size: 10px; color: #000; font-weight: 500; line-height: 1.3; }
                .qr-img { width: 110px; height: 110px; flex: 0 0 110px; object-fit: contain; }

                .invoice-card-black {
                  border: 1px solid #000;
                  border-radius: 4px;
                  padding: 6px;
                  margin: 6px 0;
                  background: #fff;
                  display: flex;
                  align-items: center;
                  gap: 8px;
                  page-break-inside: avoid;
                }
                .invoice-card-black .qr-title { color: #000; font-size: 11px; font-weight: 800; margin-bottom: 3px; letter-spacing: 0.5px; }
                .invoice-card-black .qr-sub { color: #000; font-size: 9px; font-weight: 500; line-height: 1.25; }
                .invoice-card-black .qr-img { width: 105px; height: 105px; flex-basis: 105px; }

                .footer-section { font-size: 9px; font-style: italic; margin-top: 6px; margin-bottom: 4px; line-height: 1.3; }

                @media print {
                  @page {
                    size: auto;
                    margin: 0mm;
                  }
                  html, body {
                    margin: 0;
                    padding: 0;
                    width: 100%;
                  }
                  body {
                    padding: 0 4px 4px 4px;
                  }
                  .no-print { display: none; }
                }
              </style>
            </head>
            <body>
              <div class="text-center">
                <div class="store-title">${storeTitle}</div>
                <div class="bill-title">${billTitle}</div>
              </div>

              <table class="info-table">
                <tr>
                  <td style="width: 35%;">Số HĐ:</td>
                  <td style="width: 65%; font-weight: 600;">${order.BillCode || order.OrderCode || 'N/A'}</td>
                </tr>
                ${order.OrderDate ? `
                <tr>
                  <td>Ngày HĐ:</td>
                  <td style="font-weight: 600;">${new Date(order.OrderDate).toLocaleDateString('vi-VN')}</td>
                </tr>
                ` : ''}
                <tr>
                  <td>Bàn:</td>
                  <td style="font-weight: 700;">${tableName} &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Số khách: ${order.CustomerCount || 1}</td>
                </tr>
                <tr>
                  <td>Ngày giờ:</td>
                  <td>${checkInTime}</td>
                </tr>
                <tr>
                  <td>Thu ngân:</td>
                  <td>${waiter}</td>
                </tr>
              </table>

              <div class="divider"></div>

              <table class="items-table">
                <thead>
                  <tr>
                    <th style="width: 7%; text-align: left;">STT</th>
                    <th style="width: 35%; text-align: left;">Tên món</th>
                    <th style="width: 8%; text-align: center;">SL</th>
                    <th style="width: 18%; text-align: right;">Đơn giá</th>
                    <th style="width: 12%; text-align: right;">%KM</th>
                    <th style="width: 20%; text-align: right;">Thành tiền</th>
                  </tr>
                </thead>
                <tbody>
                  ${itemsHtml}
                </tbody>
              </table>

              ${mainOrderNote ? `
              <div style="font-size: 9.5px; font-weight: 800; color: #000; border: 1px solid #000; padding: 4px 6px; margin: 5px 0; border-radius: 4px;">
                * GHI CHÚ ĐƠN: ${mainOrderNote}
              </div>
              ` : ''}

              <div class="divider"></div>

              <div class="bold" style="font-size: 9.5px; margin-bottom: 2px;">TỔNG HỢP</div>
              <table class="summary-table">
                <thead>
                  <tr style="border-bottom: 1px solid #ccc;">
                    <th style="text-align: left; width: 28%;"></th>
                    <th style="width: 22%;">Trước thuế</th>
                    <th style="width: 22%;">Thuế GTGT</th>
                    <th style="width: 28%;">Cộng tiền</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td style="text-align: left;">Thuế suất 8%</td>
                    <td>${group8.subtotal.toLocaleString('vi-VN')}</td>
                    <td>${group8.vatAmt.toLocaleString('vi-VN')}</td>
                    <td>${group8.totalAmt.toLocaleString('vi-VN')}</td>
                  </tr>
                  <tr>
                    <td style="text-align: left;">Thuế suất 10%</td>
                    <td>${group10.subtotal.toLocaleString('vi-VN')}</td>
                    <td>${group10.vatAmt.toLocaleString('vi-VN')}</td>
                    <td>${group10.totalAmt.toLocaleString('vi-VN')}</td>
                  </tr>
                  <tr>
                    <td style="text-align: left;">Thuế suất khác</td>
                    <td>${otherSubtotal.toLocaleString('vi-VN')}</td>
                    <td>${otherVatAmt.toLocaleString('vi-VN')}</td>
                    <td>${otherTotalAmt.toLocaleString('vi-VN')}</td>
                  </tr>
                </tbody>
              </table>
              ${shippingFeeAmt > 0 ? `
              <div style="display: flex; justify-content: space-between; font-weight: 700; font-size: 10px; margin: 4px 0 2px 0;">
                <span>Phí giao hàng:</span>
                <span>+${shippingFeeAmt.toLocaleString('vi-VN')}đ</span>
              </div>
              ` : ''}
              <div class="divider"></div>

              <div class="total-box">
                <span>TỔNG CỘNG</span>
                <span>${total.toLocaleString('vi-VN')}đ</span>
              </div>
              <div class="text-right" style="font-size: 9px; margin-bottom: 4px;">(Đã bao gồm thuế GTGT ${shippingFeeAmt > 0 ? '& phí giao hàng' : ''})</div>
              <div class="words-text">
                Số tiền viết bằng chữ:<br>
                <strong>${formattedTotalWords}</strong>
              </div>

              <div class="divider"></div>

              ${appliedPromoNames.length > 0 ? `
              <div style="font-size: 9.5px; margin: 4px 0 6px 0; text-align: left;">
                <div style="font-weight: 700; color: #000;">HĐ đã được KM:</div>
                ${appliedPromoNames.map(name => `<div style="padding-left: 10px; color: #000; font-size: 9.5px;">${name}</div>`).join('')}
              </div>
              ` : ''}

              <div style="font-size: 11px; font-weight: 800; font-style: italic; margin: 6px 0 6px 0; color: #000; text-align: center; line-height: 1.35;">
                Vui lòng kiểm tra kỹ lại nội dung trước khi thanh<br>toán!
              </div>

              <div class="divider"></div>

              <!-- Main Payment QR Code -->
              ${qrUrl ? `
              <div class="qr-section">
                <div class="qr-copy">
                  <div class="qr-title">THANH TOÁN</div>
                  <div class="qr-sub">Quét mã QR để thanh toán. Hỗ trợ tất cả ngân hàng và ví điện tử.</div>
                </div>
                <img class="qr-img" src="${qrUrl}" />
              </div>
              <div class="divider"></div>
              ` : ''}

              <!-- Secondary E-Invoice QR Code -->
              ${showInvoiceQr ? `
              <div class="invoice-card-black">
                <img class="qr-img" src="${invoiceQrUrl}" />
                <div class="qr-copy">
                  <div class="qr-title">XUẤT HÓA ĐƠN</div>
                  <div class="qr-sub">Quét mã QR để nhập thông tin xuất hóa đơn.<br>Vui lòng thực hiện trong vòng 2 giờ kể từ thời điểm in/thanh toán.</div>
                </div>
              </div>
              <div class="divider"></div>
              ` : ''}

              <div class="text-center" style="font-size: 10px; margin-top: 12px;">Trân trọng cảm ơn!</div>
              <div class="text-center" style="font-size: 10px; font-weight: bold;">Hệ thống Quản lý Quán Café - UTC Coffee</div>
              <div class="text-center" style="margin-top: 16px;">-</div>

            </body>
            </html>
        `;

        this.printHtmlViaHiddenIframe(contentHtml);
    }

    close() {
        this.ref.close();
    }
}
