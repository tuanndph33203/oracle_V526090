import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HeaderBreadcumb } from './header-breadcumb';

describe('HeaderBreadcumb', () => {
  let component: HeaderBreadcumb;
  let fixture: ComponentFixture<HeaderBreadcumb>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HeaderBreadcumb]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HeaderBreadcumb);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
