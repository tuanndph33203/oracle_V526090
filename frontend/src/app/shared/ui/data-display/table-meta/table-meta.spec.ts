import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TableMeta } from './table-meta';

describe('TableMeta', () => {
  let component: TableMeta;
  let fixture: ComponentFixture<TableMeta>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TableMeta]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TableMeta);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
