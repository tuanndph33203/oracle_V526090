import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FileUploadMeta } from './file-upload-meta';

describe('FileUploadMeta', () => {
  let component: FileUploadMeta;
  let fixture: ComponentFixture<FileUploadMeta>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FileUploadMeta]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FileUploadMeta);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
