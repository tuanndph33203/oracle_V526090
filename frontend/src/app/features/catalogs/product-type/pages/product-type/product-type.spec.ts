import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductType } from './product-type';

describe('ProductType', () => {
  let component: ProductType;
  let fixture: ComponentFixture<ProductType>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductType]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductType);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
