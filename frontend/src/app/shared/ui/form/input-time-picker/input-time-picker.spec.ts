import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InputTimePicker } from './input-time-picker';

describe('InputTimePicker', () => {
    let component: InputTimePicker;
    let fixture: ComponentFixture<InputTimePicker>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [InputTimePicker]
        }).compileComponents();

        fixture = TestBed.createComponent(InputTimePicker);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
