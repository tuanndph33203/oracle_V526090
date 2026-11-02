import { LayoutService } from '@/core/services/layout';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterModule, RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { DialogService } from 'primeng/dynamicdialog';
import { ToastModule } from 'primeng/toast';
@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule, ConfirmDialog, ToastModule, CommonModule],
    template: `<div>
        <router-outlet></router-outlet>
        <p-toast [position]="'top-right'" [showTransformOptions]="'translateY(100%)'" [showTransitionOptions]="'300ms'" [hideTransitionOptions]="'300ms'" />
        <p-confirmDialog key="delete-data"></p-confirmDialog>
        <p-confirmDialog key="leave-form"></p-confirmDialog>
    </div> `,

    providers: [ConfirmationService, DialogService]
})
export class AppComponent extends TitleStrategy {
    private layoutService = inject(LayoutService);
    private title = inject(Title);

    override updateTitle(snapshot: RouterStateSnapshot) {
        const title = this.buildTitle(snapshot);
        if (title) {
            this.title.setTitle(title);
        }
    }

    ngOnInit(): void {
        setTimeout(() => {
            this.layoutService.viewReady.set(true);
        }, 1000);
    }
}
