import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NavigationCreate } from './navigation-create';

describe('NavigationCreate', () => {
  let component: NavigationCreate;
  let fixture: ComponentFixture<NavigationCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavigationCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NavigationCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
