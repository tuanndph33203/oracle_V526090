import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DialogWrap } from './dialog-wrap';

describe('DialogWrap', () => {
  let component: DialogWrap;
  let fixture: ComponentFixture<DialogWrap>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DialogWrap]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DialogWrap);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
