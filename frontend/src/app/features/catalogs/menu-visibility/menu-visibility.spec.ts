import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { selectAvailableBranchCodes, selectSelectedBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import { Store } from '@ngrx/store';
import { BehaviorSubject, of, Subject } from 'rxjs';
import { BaseHttpService } from '@/core/services/base-http';
import { MenuVisibility } from './menu-visibility';
import { MessageService } from 'primeng/api';

describe('MenuVisibility', () => {
    let fixture: ComponentFixture<MenuVisibility>;
    let component: MenuVisibility;
    let selectedBranches: BehaviorSubject<{ Code: string; Name: string }[]>;
    let api: { get: jasmine.Spy; post: jasmine.Spy; put: jasmine.Spy; patch: jasmine.Spy; delete: jasmine.Spy };
    const rule = { Id: 'rule1', BranchCode: 'HN', IsEnabled: true, MenuItemIds: ['m1'], Schedules: [{ Kind: 'Weekdays', Days: [1], StartDate: '', EndDate: '', TimeRanges: [{ From: '11:00', To: '13:30' }] }] };
    beforeEach(async () => {
        selectedBranches = new BehaviorSubject([{ Code: 'HN', Name: 'CN Hà Nội' }]);
        api = {
            get: jasmine.createSpy().and.callFake((url: string) => of({ Code: 200, Data: url === 'menu-visibility/items' ? [
                { Id: 'm1', Name: 'Phở gà', Code: 'PHOGA', BranchIds: ['b1'] },
                { Id: 'm2', Name: 'Bún bò', Code: 'BUNBO', BranchIds: ['b2'] },
                { Id: 'm3', Name: 'Trà đá', Code: 'TRA', BranchIds: [] }
            ] : [structuredClone(rule)] })),
            post: jasmine.createSpy().and.callFake((_url, data) => of({ Code: 200, Data: { ...data, Id: 'created' } })),
            put: jasmine.createSpy().and.callFake((_url, data) => of({ Code: 200, Data: data })),
            patch: jasmine.createSpy().and.returnValue(of({ Code: 200 })),
            delete: jasmine.createSpy().and.returnValue(of({ Code: 200 }))
        };
        await TestBed.configureTestingModule({
            imports: [MenuVisibility], providers: [provideRouter([]), provideNoopAnimations(),
                { provide: BaseHttpService, useValue: api },
                { provide: Store, useValue: { select: (selector: unknown) => selector === selectSelectedBranchCodes ? selectedBranches.asObservable() : of(selector === selectAvailableBranchCodes ? [{ Id: 'b1', Code: 'HN', Name: 'CN Hà Nội' }, { Id: 'b2', Code: 'HCM', Name: 'CN Hồ Chí Minh' }] : []) } },
            ]
        }).compileComponents();
        fixture = TestBed.createComponent(MenuVisibility);
        component = fixture.componentInstance;
        fixture.detectChanges(); await fixture.whenStable(); fixture.detectChanges();
    });
    it('opens the create view from the list and filters items by branch', async () => {
        const button = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(b => b.textContent?.includes('Thêm mới'))!;
        button.click(); fixture.detectChanges(); await fixture.whenStable();
        expect(fixture.nativeElement.querySelector('#visibility-branch')).toBeTruthy();
        expect(component.draft!.BranchCode).toBe('HN');
        expect(component.selectedBranches.map(branch => branch.Code)).toEqual(['HN']);
        expect(component.visibleItems.map(i => i.Id)).toEqual(['m1', 'm3']);
        selectedBranches.next([{ Code: 'HCM', Name: 'CN Hồ Chí Minh' }]);
        component.draft!.BranchCode = 'HCM'; component.changeBranch();
        expect(component.visibleItems.map(i => i.Id)).toEqual(['m2', 'm3']);
        expect(component.draft!.MenuItemIds).toEqual([]);
    });
    it('shows only rules from the branches selected in the topbar', () => {
        component.rules = [component.rules[0], { ...structuredClone(component.rules[0]), Id: 'rule2', BranchCode: 'HCM' }];
        expect(component.filteredRules.map(rule => rule.Id)).toEqual(['rule1']);
        selectedBranches.next([{ Code: 'HCM', Name: 'CN Hồ Chí Minh' }]);
        expect(component.filteredRules.map(rule => rule.Id)).toEqual(['rule2']);
        component.start();
        expect(component.draft!.BranchCode).toBe('HCM');
        expect(component.selectedBranches.map(branch => branch.Code)).toEqual(['HCM']);
    });
    it('finishes loading the rules while item options are still pending', async () => {
        const pendingItems = new Subject<unknown>();
        component.items = [];
        api.get.and.callFake((url: string) => url === 'menu-visibility/items'
            ? pendingItems.asObservable() : of({ Code: 200, Data: [structuredClone(rule)] }));
        await component.load();
        expect(component.loading).toBeFalse();
        expect(component.loadingItems).toBeTrue();
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('app-loading')).toBeNull();
        pendingItems.next({ Code: 200, Data: [] }); pendingItems.complete();
        await fixture.whenStable();
        expect(component.loadingItems).toBeFalse();
    });
    it('keeps table input stable across change detection and refreshes it after a status change', async () => {
        const rows = component.tableRows;
        expect(component.filteredRules).toBe(component.filteredRules);
        expect(component.tableRows).toBe(rows);
        fixture.detectChanges();
        expect(component.tableRows).toBe(rows);
        await component.toggle(component.rules[0]);
        expect(component.tableRows).not.toBe(rows);
        expect(component.tableRows[0].IsEnabled).toBeFalse();
    });
    it('selects only search results and includes a pending time range on save', async () => {
        component.start(); component.itemSearch = 'phoga'; component.toggleAll(true);
        expect(component.draft!.MenuItemIds).toEqual(['m1']);
        const schedule = component.draft!.Schedules[0];
        schedule.Days = [1, 5]; schedule.draftFrom = '22:00'; schedule.draftTo = '02:00';
        await component.save();
        const payload = api.post.calls.mostRecent().args[1];
        expect(payload.Schedules[0].TimeRanges).toEqual([{ From: '22:00', To: '02:00' }]);
        expect(payload.Schedules[0].draftFrom).toBeUndefined();
        expect(component.draft).toBeNull();
        expect(component.rules[0].Id).toBe('created');
    });
    it('cancelling an edit does not mutate saved rules', () => {
        component.start(component.rules[0]); component.draft!.Schedules[0].Days = [6]; component.back();
        expect(component.rules[0].Schedules[0].Days).toEqual([1]);
    });
    it('retains the draft when the server rejects it', async () => {
        api.put.and.returnValue(of({ Code: 400, Message: 'Món không thuộc chi nhánh.' }));
        component.start(component.rules[0]); await component.save();
        expect(component.draft).not.toBeNull(); expect(component.error).toContain('chi nhánh');
    });
    it('rejects invalid dates before contacting the API', async () => {
        component.start(component.rules[0]);
        Object.assign(component.draft!.Schedules[0], { Kind: 'DateRange', StartDate: '2026-10-28', EndDate: '2026-10-26' });
        await component.save(); expect(api.put).not.toHaveBeenCalled(); expect(component.error).toBeTruthy();
    });
    it('marks required fields and reports validation errors as toasts', async () => {
        const messages = spyOn(fixture.debugElement.injector.get(MessageService), 'add');
        component.start();
        await component.save();
        expect(component.submitted).toBeTrue();
        expect(component.error).toContain('món');
        expect(messages).toHaveBeenCalled();
        expect(api.post).not.toHaveBeenCalled();

        component.toggleItem('m1', true);
        await component.save();
        expect(component.error).toContain('ngày ẩn');

        const schedule = component.draft!.Schedules[0];
        schedule.Days = [1]; schedule.draftFrom = '10:00';
        await component.save();
        expect(component.error).toContain('từ giờ và đến giờ');
        expect(api.post).not.toHaveBeenCalled();
    });
    it('does not change local status or delete the rule after a rejected request', async () => {
        api.patch.and.returnValue(of({ Code: 500, Message: 'Thất bại' }));
        await component.toggle(component.rules[0]); expect(component.rules[0].IsEnabled).toBeTrue();
        api.delete.and.returnValue(of({ Code: 500, Message: 'Thất bại' }));
        component.pendingDelete = component.rules[0]; await component.remove(); expect(component.rules.length).toBe(1);
    });
});
