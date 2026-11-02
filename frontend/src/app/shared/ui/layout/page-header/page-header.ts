import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Toolbar } from 'primeng/toolbar';

@Component({
    selector: 'app-page-header',
    imports: [CommonModule, Toolbar],
    templateUrl: './page-header.html',
    styleUrl: './page-header.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PageHeader {
    title = input<string>('');
    description = input<string>('');
    icon = input<string>('');
}
