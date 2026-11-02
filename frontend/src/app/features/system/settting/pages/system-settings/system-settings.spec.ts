import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';
import { SystemSettingsService } from '../../services/system-settings';
import { SystemSettings } from './system-settings';

describe('SystemSettings', () => {
    let component: SystemSettings;
    let fixture: ComponentFixture<SystemSettings>;
    let systemSettingsService: {
        getAll: jasmine.Spy;
        updateBulk: jasmine.Spy;
    };
    let messageService: {
        add: jasmine.Spy;
    };

    beforeEach(async () => {
        systemSettingsService = {
            getAll: jasmine.createSpy('getAll').and.returnValue(of({ Data: [] })),
            updateBulk: jasmine.createSpy('updateBulk').and.returnValue(of({ Code: 200 }))
        };
        messageService = {
            add: jasmine.createSpy('add')
        };

        await TestBed.configureTestingModule({
            imports: [SystemSettings],
            providers: [
                {
                    provide: SystemSettingsService,
                    useValue: systemSettingsService
                },
                {
                    provide: MessageService,
                    useValue: messageService
                }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(SystemSettings);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should build settings from FE config when API returns empty data', () => {
        expect(component.settingGroups().length).toBe(2);
        expect(component.settingGroups()[0].module).toBe('SaleBill');
        expect(component.settingGroups()[0].items[0].Code).toBe('sale-bill-manual-tax');
        expect(component.missingCodes().length).toBe(11);
    });

    it('should map API items by code and save only mapped settings', () => {
        systemSettingsService.getAll.and.returnValue(
            of({
                Data: [
                    {
                        Id: '7aa91fa0-d262-4fa9-8147-317e55fd4d95',
                        Code: 'sale-bill-manual-tax',
                        Name: 'Thuế thủ công',
                        Value: 'false',
                        DataType: 'Boolean',
                        Module: 'SaleBill',
                        Description: '',
                        IsActive: false
                    }
                ]
            })
        );

        fixture = TestBed.createComponent(SystemSettings);
        component = fixture.componentInstance;
        fixture.detectChanges();

        const group = component.settingGroups()[0];
        const item = group.items.find((setting) => setting.Code === 'sale-bill-manual-tax');

        expect(item?._checked).toBeFalse();
        expect(component.missingCodes().length).toBe(10);

        item!._checked = true;
        component.onToggle(item!);
        component.onSave();

        expect(systemSettingsService.updateBulk).toHaveBeenCalledWith({
            Items: [
                {
                    Id: '7aa91fa0-d262-4fa9-8147-317e55fd4d95',
                    Name: 'Thuế thủ công',
                    Value: 'true',
                    DataType: 'Boolean',
                    Module: 'SaleBill',
                    Description: '',
                    IsActive: true
                }
            ]
        });
        expect(messageService.add).toHaveBeenCalledWith(
            jasmine.objectContaining({
                severity: 'success'
            })
        );
    });
});
