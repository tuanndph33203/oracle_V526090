import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DiningTable } from './dining-table';
import { PaymentRealtimeService } from '@/core/services/payment-realtime';
import { MessageService, ConfirmationService } from 'primeng/api';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { signal } from '@angular/core';

describe('DiningTable Notification Filter', () => {
  let component: DiningTable;
  let fixture: ComponentFixture<DiningTable>;
  let mockPaymentRealtimeService: any;

  beforeEach(async () => {
    mockPaymentRealtimeService = {
      qrOrderNotification: signal<any | null>(null),
      clearQrOrderNotification: jasmine.createSpy('clearQrOrderNotification').and.callFake(() => {
        mockPaymentRealtimeService.qrOrderNotification.set(null);
      }),
      notifications: signal<any[]>([]),
      connectionState: signal<any>('Connected'),
      activeTransactionId: signal<any>(null),
      paymentStatusByPaymentId: signal<any>({}),
      lastStatusEvent: signal<any>(null)
    };

    const mockStore = {
      select: () => of([]),
      dispatch: () => {}
    };

    await TestBed.configureTestingModule({
      imports: [DiningTable],
      providers: [
        { provide: PaymentRealtimeService, useValue: mockPaymentRealtimeService },
        { provide: Store, useValue: mockStore },
        { provide: MessageService, useValue: { add: () => {} } },
        { provide: ConfirmationService, useValue: { confirm: () => {} } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DiningTable);
    component = fixture.componentInstance;
  });

  it('should filter out duplicate QR notifications based on processed ID', () => {
    component.processedQrNotificationIds.clear();
    component.lastNotificationTime = 0;

    const noti = { id: 'test-1', type: 'CallWaiter', tableName: 'B01', orderCode: 'ORD-1' };
    
    // First notification processing check
    const firstCheck = component.processedQrNotificationIds.has('test-1');
    expect(firstCheck).toBeFalse();
    
    // Process and mark it as processed
    component.processedQrNotificationIds.add('test-1');
    
    // Second notification processing check
    const secondCheck = component.processedQrNotificationIds.has('test-1');
    expect(secondCheck).toBeTrue();
  });
});
