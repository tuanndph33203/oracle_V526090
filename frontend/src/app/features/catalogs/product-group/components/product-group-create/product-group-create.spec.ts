import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductGroupCreate } from './product-group-create';

describe('ProductGroupCreate', () => {
  let component: ProductGroupCreate;
  let fixture: ComponentFixture<ProductGroupCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductGroupCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProductGroupCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
