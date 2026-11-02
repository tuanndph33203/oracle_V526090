import { HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseHttpService } from './base-http';

export interface ApiResponse<T = any> {
    Code: number;
    Message: string;
    TotalTime?: number;
    DataCount?: number;
    Status?: number;
    TotalCount?: number;
    Data?: T;
}

@Injectable({
    providedIn: 'root'
})
export class MinioService {
    private http = inject(BaseHttpService);
    private baseUrl = 'minio';

    upload(file: File): Observable<ApiResponse<any>> {
        const formData = new FormData();
        formData.append('file', file);

        return this.http.post<ApiResponse>(`${this.baseUrl}/upload`, formData);
    }

    uploadMany(files: File[], folder?: string): Observable<ApiResponse<any>> {
        const formData = new FormData();
        files.forEach((file) => formData.append('files', file));

        return this.http.post<ApiResponse>(`${this.baseUrl}/upload-many`, formData, folder ? { folder } : undefined);
    }

    download(objectName: string): Observable<Blob> {
        const params = new HttpParams().set('objectName', objectName);

        return this.http.get(`${this.baseUrl}/download`, {
            params,
            responseType: 'blob'
        });
    }

    delete(objectName: string): Observable<ApiResponse> {
        const params = new HttpParams().set('objectName', objectName);

        return this.http.delete<ApiResponse>(`${this.baseUrl}`, { params });
    }
}
