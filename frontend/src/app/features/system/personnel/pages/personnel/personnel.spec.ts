import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Personnel } from './personnel';

describe('Personnel', () => {
  let component: Personnel;
  let fixture: ComponentFixture<Personnel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Personnel]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Personnel);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
