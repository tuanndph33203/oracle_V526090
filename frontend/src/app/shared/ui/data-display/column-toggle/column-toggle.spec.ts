import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ColumnToggle } from './column-toggle';

describe('ColumnToggle', () => {
  let component: ColumnToggle;
  let fixture: ComponentFixture<ColumnToggle>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ColumnToggle]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ColumnToggle);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
