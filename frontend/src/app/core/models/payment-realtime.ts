export type PaymentLifecycleStatus = 1 | 2;

export interface ICreatePaymentTransactionRequest {
    TransactionId: string;
    BranchCode: string;
    AmountDue: number;
    CustomerCode?: string;
    CustomerName?: string;
    CreatedByUserName: string;
    CreatedByCode: string;
}

export interface IPaymentWebhookRequest {
    transaction_key: string;
    amount: number;
    merchant_transaction_id: string;
    status: number;
}

export interface IPaymentStatusEvent {
    orderId: string;
    paymentId: string;
    transactionId: string;
    status: PaymentLifecycleStatus | null;
    message?: string;
    updatedAt?: string;
    amountDue?: number | null;
    amountPaid?: number | null;
}

export interface ISalesRealtimeEvent {
    eventType: 'OrderConfirmed' | 'QrOrderMerged' | 'TableStatusChanged' | 'TableOpened' | 'TableTransferred' | 'TablesMerged' | 'OrderPaid' | 'OrderCancelled' | 'OrderUpdated' | string;
    orderId?: string;
    orderCode?: string;
    tableId?: string;
    tableName?: string;
    newStatus?: string;
    serviceType?: string;
    actionBy?: string;
    timestamp?: string;
}
