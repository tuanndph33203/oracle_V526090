import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductTypeCreate } from './product-type-create';

describe('ProductTypeCreate', () => {
  let component: ProductTypeCreate;
  let fixture: ComponentFixture<ProductTypeCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductTypeCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductTypeCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
