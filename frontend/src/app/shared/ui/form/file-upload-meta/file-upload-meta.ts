import { Component, EventEmitter, forwardRef, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MessageService } from 'primeng/api';
import { PrimeNG } from 'primeng/config';
import { FileSelectEvent, FileUpload } from 'primeng/fileupload';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Image } from 'primeng/image';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-file-upload-meta',
    imports: [FileUpload, ButtonModule, BadgeModule, CommonModule, FormsModule, Image],
    templateUrl: './file-upload-meta.html',
    styleUrl: './file-upload-meta.scss',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => FileUploadMeta),
            multi: true
        }
    ]
})
export class FileUploadMeta {
    @ViewChild('fu') fileUpload!: FileUpload;

    apiUrl = environment.apiUrl;
    fileLink : string[] = [];

    files: File[] = [];
    disabled = false;
    totalSize = 0;
    totalSizePercent = 0;

    @Output() uploaded = new EventEmitter<File[]>();

    constructor(
        private config: PrimeNG,
        private messageService: MessageService
    ) {}

    private onChange: (value: File[]) => void = () => {};
    private onTouched: () => void = () => {};

    writeValue(value: any): void {
        if (!value) {
            this.fileLink = [];
        } else if (Array.isArray(value)) {
            this.fileLink = value.filter(Boolean);
        } else if (typeof value === 'string') {
            this.fileLink = [value];
        } else {
            this.fileLink = [];
        }
    }

    getImageUrl(url: string | null | undefined): string {
        if (!url) {
            return '';
        }
        if (url.startsWith('http://') || url.startsWith('https://')) {
            return url;
        }
        return `${this.apiUrl}minio/download?objectName=${encodeURIComponent(url)}`;
    }

    onRemoveOldFile(event: Event, index: number) {
        event.stopPropagation();
        this.fileLink.splice(index, 1);
        this.onChange(null as any);
        this.onTouched();
    }

    registerOnChange(fn: (value: File[]) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    openFileChooser() {
        if (!this.disabled) {
            this.fileUpload.choose();
        }
    }

    onSelectedFiles(event: FileSelectEvent) {
        this.files = event.currentFiles;
        this.recalculateSize();
        this.onChange(this.files);
        this.onTouched();

        this.uploaded.emit(this.files);
        this.messageService.add({
            severity: 'success',
            summary: 'Thành công',
            detail: 'Tải ảnh lên thành công',
            life: 2000
        });
    }

    onRemoveTemplatingFile(event: Event, removeFileCallback: Function, index: number) {
        removeFileCallback(event, index);
        this.files.splice(index, 1);
        this.recalculateSize();
        this.onChange(this.files);
        this.onTouched();
    }

    formatSize(bytes: number) {
        const k = 1024;
        const dm = 3;
        const sizes = this.config.translation.fileSizeTypes ?? [];
        if (bytes === 0) {
            return `0 ${sizes[0]}`;
        }

        const i = Math.floor(Math.log(bytes) / Math.log(k));
        const formattedSize = parseFloat((bytes / Math.pow(k, i)).toFixed(dm));

        return `${formattedSize} ${sizes[i]}`;
    }

    onClear() {
        this.files = [];
        this.totalSize = 0;
        this.totalSizePercent = 0;

        this.fileUpload.clear();

        this.onChange(this.files);
        this.onTouched();
    }

    private recalculateSize() {
        this.totalSize = this.files.reduce((sum, f) => sum + f.size, 0);
        this.totalSizePercent = (this.totalSize / 1_000_000) * 100;
    }
}
