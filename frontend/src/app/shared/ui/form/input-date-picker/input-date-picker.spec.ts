import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InputDatePicker } from './input-date-picker';

describe('InputDatePicker', () => {
  let component: InputDatePicker;
  let fixture: ComponentFixture<InputDatePicker>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InputDatePicker]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InputDatePicker);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
