import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PersonnelCreate } from './personnel-create';

describe('PersonnelCreate', () => {
  let component: PersonnelCreate;
  let fixture: ComponentFixture<PersonnelCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PersonnelCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PersonnelCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
