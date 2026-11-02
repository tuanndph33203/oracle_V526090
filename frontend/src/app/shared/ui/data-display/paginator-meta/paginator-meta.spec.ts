import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PaginatorMeta } from './paginator-meta';

describe('PaginatorMeta', () => {
  let component: PaginatorMeta;
  let fixture: ComponentFixture<PaginatorMeta>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaginatorMeta]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PaginatorMeta);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
