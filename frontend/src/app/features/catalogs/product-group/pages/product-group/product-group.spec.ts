import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductGroup } from './product-group';

describe('ProductGroup', () => {
  let component: ProductGroup;
  let fixture: ComponentFixture<ProductGroup>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductGroup]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductGroup);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
