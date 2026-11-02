export interface CalculatedItemResult {
    unitDiscount: number;
    unitPreTax: number;
    unitVat: number;
    unitTotal: number;
    amount: number;
    discountAmount: number;
    vatAmount: number;
    totalAmount: number;
}

export interface CalculateItemParams {
    qty: number;
    unitPrice: number;
    discountRate?: number;
    itemDiscount?: number;
    itemOrderDiscount?: number;
    vatRate?: number;
    fixedTax?: number;
    basePrice?: number;
}

export interface TaxGroupSummary {
    rate: number;
    subtotal: number;
    vatAmt: number;
    totalAmt: number;
}

/**
 * Standardized order & item calculation utility aligning Frontend with Backend rules.
 * Uses unit price pre-tax (unitPreTax) and unit VAT (unitVat) before multiplying by quantity.
 */
export class OrderCalculator {
    /**
     * Calculate discount, pre-tax, VAT, and total for a single item line.
     * New formula:
     * 1. Thuế = Giá trước thuế * % thuế (hoặc fixedTax)
     * 2. Thành tiền sau thuế (Gross) = Giá trước thuế + Thuế
     * 3. Chiết khấu = Thành tiền sau thuế * % chiết khấu
     * 4. Thành tiền sau thuế sau CK từng món = Thành tiền sau thuế - Chiết khấu
     */
    static calculateItem(params: CalculateItemParams): CalculatedItemResult {
        const qty = Math.max(0, params.qty || 0);
        const effectiveQty = qty > 0 ? qty : 1;
        const unitPrice = params.unitPrice || 0;

        let unitVat = 0;
        if (params.fixedTax && params.fixedTax > 0) {
            const extraPrice = (params.basePrice !== undefined && params.unitPrice > params.basePrice)
                ? params.unitPrice - params.basePrice
                : 0;
            const extraVat = (extraPrice > 0 && params.vatRate && params.vatRate > 0)
                ? Math.round(extraPrice * (params.vatRate / 100))
                : 0;
            unitVat = Math.round(params.fixedTax) + extraVat;
        } else {
            unitVat = Math.round(unitPrice * ((params.vatRate || 0) / 100));
        }

        const unitGross = unitPrice + unitVat;

        let itemDiscount = params.itemDiscount || 0;
        if (params.discountRate && params.discountRate > 0 && !params.itemDiscount) {
            const unitDisc = Math.round(unitGross * (params.discountRate / 100));
            itemDiscount = unitDisc * effectiveQty;
        }

        const itemOrderDiscount = params.itemOrderDiscount || 0;
        const totalDiscount = itemDiscount + itemOrderDiscount;

        const unitDiscount = Math.round(totalDiscount / effectiveQty);
        const unitTotal = Math.max(0, unitGross - unitDiscount);
        const unitPreTax = unitPrice;

        const isFullyDiscounted = unitTotal <= 0 || (params.discountRate !== undefined && params.discountRate >= 100);

        return {
            unitDiscount: isFullyDiscounted ? unitGross : unitDiscount,
            unitPreTax,
            unitVat: isFullyDiscounted ? 0 : unitVat,
            unitTotal: isFullyDiscounted ? 0 : unitTotal,
            amount: Math.round(unitPrice * qty),
            discountAmount: isFullyDiscounted ? Math.round(unitGross * effectiveQty) : Math.round(unitDiscount * effectiveQty),
            vatAmount: isFullyDiscounted ? 0 : Math.round(unitVat * qty),
            totalAmount: isFullyDiscounted ? 0 : Math.round(unitTotal * qty)
        };
    }

    /**
     * Group items by VAT rate (e.g. 8%, 10%) for receipt printing & tax breakdown previews.
     */
    static calculateTaxGroups(items: any[], getQtyFn?: (item: any) => number): {
        taxGroups: { [key: string]: TaxGroupSummary };
        subtotal: number;
        discount: number;
        vat: number;
        total: number;
    } {
        const taxGroups: { [key: string]: TaxGroupSummary } = {};
        let subtotal = 0;
        let discount = 0;
        let vat = 0;
        let total = 0;

        (items || []).forEach(item => {
            const qty = getQtyFn ? getQtyFn(item) : (item.Quantity || 0);
            if (qty <= 0) return;

            const unitPrice = item.Price !== undefined ? item.Price : (item.UnitPrice !== undefined ? item.UnitPrice : 0);
            const vatRate = item.VatRate || 0;
            const fixedTax = item.FixedTaxAmount !== undefined ? item.FixedTaxAmount : (item.fixedTaxAmount || 0);
            const unitVat = fixedTax > 0 ? Math.round(fixedTax) : Math.round(unitPrice * (vatRate / 100));
            const unitGross = unitPrice + unitVat;

            let itemDisc = 0;
            if (item.DiscountRate > 0) {
                const unitDisc = Math.round(unitGross * (item.DiscountRate / 100));
                itemDisc = unitDisc * qty;
            } else if (item.DiscountAmount !== undefined && item.DiscountAmount !== null && item.DiscountAmount > 0) {
                itemDisc = item.DiscountAmount;
            }

            const calc = this.calculateItem({
                qty,
                unitPrice,
                itemDiscount: itemDisc,
                vatRate,
                fixedTax
            });

            const key = vatRate + '%';
            if (!taxGroups[key]) {
                taxGroups[key] = { rate: vatRate, subtotal: 0, vatAmt: 0, totalAmt: 0 };
            }

            taxGroups[key].subtotal += calc.amount;
            taxGroups[key].vatAmt += calc.vatAmount;
            taxGroups[key].totalAmt += calc.totalAmount;

            subtotal += calc.amount;
            discount += calc.discountAmount;
            vat += calc.vatAmount;
            total += calc.totalAmount;
        });

        return { taxGroups, subtotal, discount, vat, total };
    }
}
