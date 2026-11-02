import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-dialog-wrap',
    imports: [ButtonModule],
    templateUrl: './dialog-wrap.html',
    styleUrl: './dialog-wrap.scss'
})
export class DialogWrap {
    title = input<string>('');
    close = output<void>();

    onClose() {
        this.close.emit();
    }
}
