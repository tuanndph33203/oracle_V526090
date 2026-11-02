import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountCreate } from './account-create';

describe('AccountCreate', () => {
  let component: AccountCreate;
  let fixture: ComponentFixture<AccountCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AccountCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AccountCreate);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
