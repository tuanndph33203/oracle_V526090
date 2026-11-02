import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrderCalculator } from '@/core/utils/order-calculator';

@Component({
    selector: 'app-selected-order-items',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './selected-order-items.html',
    styleUrl: './selected-order-items.scss'
})
export class SelectedOrderItemsComponent {
    @Input() orderItems: any[] = [];
    @Input() isBillEditUnlocked: boolean = false;
    @Input() billId: string | null = null;
    @Input() showDeleteButton: boolean = false;
    @Input() requireKitchenSend: boolean = true;

    @Output() decreaseQty = new EventEmitter<any>();
    @Output() increaseQty = new EventEmitter<any>();
    @Output() removeItem = new EventEmitter<any>();
    @Output() cancelItem = new EventEmitter<any>();
    @Output() changeQty = new EventEmitter<{ item: any, quantity: number }>();

    onQtyInputChange(item: any, event: any) {
        const val = event?.target?.value;
        let qty = Math.floor(Number(val));
        if (isNaN(qty) || qty < 1) {
            qty = 1;
        }
        item.Quantity = qty;
        if (event?.target) {
            event.target.value = qty;
        }
        this.changeQty.emit({ item, quantity: qty });
    }

    getKitchenStatusText(item: any): string {
        if (!item.SentQty && !item.ServedQty) return 'Chưa gửi';
        const sentQty = item.SentQty || item.ServedQty || 0;
        let text = `Đã gửi: ${sentQty}`;
        if (item.SoldOutQty > 0) text += ` | Hết (${item.SoldOutQty})`;
        const cancelled = item.CancelledQuantity || item.cancelledQuantity || item.CancelQuantity || 0;
        if (cancelled > 0 && !item.SoldOutQty) text += ` | Hủy (${cancelled})`;
        return text;
    }

    getKitchenStatusClass(item: any): string {
        if (!item.SentQty) return 'bg-surface-100 text-surface-600 border border-surface-200 dark:bg-surface-800 dark:text-surface-400 dark:border-surface-700';
        const cancelled = item.CancelledQuantity || item.cancelledQuantity || item.CancelQuantity || 0;
        if (item.KitchenStatus === 'FullyServed') return 'bg-green-50 text-green-700 border border-green-200 dark:bg-green-950/20 dark:text-green-400 dark:border-green-800';
        if (item.KitchenStatus === 'FullySoldOut' || item.KitchenStatus === 'Cancelled' || (cancelled > 0 && cancelled >= item.Quantity)) return 'bg-red-50 text-red-700 border border-red-200 dark:bg-red-950/20 dark:text-red-400 dark:border-red-800';
        if (item.KitchenStatus === 'PartiallyServed' || item.KitchenStatus === 'PartiallySoldOut' || cancelled > 0) return 'bg-amber-50 text-amber-700 border border-amber-200 dark:bg-amber-950/20 dark:text-amber-400 dark:border-amber-800';
        return 'bg-blue-50 text-blue-700 border border-blue-200 dark:bg-blue-950/20 dark:text-blue-400 dark:border-blue-800';
    }

    getActualUsedQty(item: any): number {
        if (!item) return 0;
        if (item.IsCancelled === 1 || item.isCancelled === 1 || item.KitchenStatus === 'Cancelled') return 0;
        if (item.IsChecked === 1 && item.ActualQuantity !== undefined && item.ActualQuantity !== null) {
            return item.ActualQuantity;
        }
        const orderQty = item.Quantity || 0;
        const soldOut = item.SoldOutQty || 0;
        const returned = item.ReturnedQuantity || 0;
        const cancelled = item.CancelledQuantity || item.cancelledQuantity || item.CancelQuantity || 0;
        return Math.max(0, orderQty - soldOut - returned - cancelled);
    }

    getItemTotalAmount(item: any): number {
        if (!item) return 0;
        const actualQty = this.getActualUsedQty(item);
        const unitPrice = item.Price !== undefined ? item.Price : (item.UnitPrice !== undefined ? item.UnitPrice : 0);
        const vatRate = item.VatRate || 0;
        const fixedTax = item.FixedTaxAmount !== undefined ? item.FixedTaxAmount : (item.fixedTaxAmount || 0);
        const hasCalculatedPromotionDiscount =
            !!(item.PromotionCode || item.promotionCode) &&
            item.DiscountAmount !== undefined &&
            item.DiscountAmount !== null;

        const calc = OrderCalculator.calculateItem({
            qty: actualQty,
            unitPrice,
            itemDiscount: hasCalculatedPromotionDiscount
                ? item.DiscountAmount
                : (item.DiscountRate > 0 ? 0 : item.DiscountAmount),
            discountRate: hasCalculatedPromotionDiscount ? 0 : item.DiscountRate,
            vatRate,
            fixedTax
        });

        return calc.totalAmount;
    }

    canDecreaseItem(item: any): boolean {
        if (!item) return false;
        return item.Quantity > 0;
    }

    canDeleteItem(item: any): boolean {
        if (!item) return false;
        return true;
    }
}
