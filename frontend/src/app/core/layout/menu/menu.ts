import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppMenuitem } from './menuitem.directive';
import { MenuService } from '@/core/services/menu';
@Component({
    selector: 'app-menu',
    imports: [CommonModule, RouterModule, AppMenuitem],
    templateUrl: './menu.html',
    styleUrl: './menu.scss'
})
export class Menu {
    readonly menu = inject(MenuService).menu;
}
