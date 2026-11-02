import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TreeTableMeta } from './tree-table-meta';

describe('TreeTableMeta', () => {
  let component: TreeTableMeta;
  let fixture: ComponentFixture<TreeTableMeta>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TreeTableMeta]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TreeTableMeta);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
