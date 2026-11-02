import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
    selector: 'app-loading',
    imports: [CommonModule, ProgressSpinnerModule],
    templateUrl: './loading.html',
    styleUrl: './loading.scss'
})
export class Loading implements OnInit {
    loadingMessage = '';
    showTroubleshootGuide = false;

    ngOnInit() {
        setTimeout(() => {
            this.loadingMessage = 'Đang tải dữ liệu...';
        }, 5000);

        setTimeout(() => {
            this.loadingMessage = 'Kết nối gặp sự cố, vui lòng chờ thêm hoặc thử làm mới lại trang.';
            this.showTroubleshootGuide = true;
        }, 15000);
    }
}
