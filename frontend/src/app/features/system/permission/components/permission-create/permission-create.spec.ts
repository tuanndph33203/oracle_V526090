import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PermissionCreate } from './permission-create';

describe('PermissionCreate', () => {
  let component: PermissionCreate;
  let fixture: ComponentFixture<PermissionCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PermissionCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PermissionCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
