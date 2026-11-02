import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Breadcrumb } from 'primeng/breadcrumb';

@Component({
    selector: 'app-header-breadcumb',
    imports: [CommonModule, Breadcrumb],
    templateUrl: './header-breadcumb.html',
    styleUrl: './header-breadcumb.scss'
})
export class HeaderBreadcumb {
    title = input<string>('');
    items = input<MenuItem[]>([]);
    home = input<MenuItem | undefined>(undefined);
}
