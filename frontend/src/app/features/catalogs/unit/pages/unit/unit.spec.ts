import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Unit } from './unit';

describe('Unit', () => {
  let component: Unit;
  let fixture: ComponentFixture<Unit>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Unit]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Unit);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
