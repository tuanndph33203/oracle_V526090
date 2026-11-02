import { FloatingConfigurator } from '@/core/layout/floating-configurator/floating-configurator';
import { Auth } from '@/core/services/auth';
import AuthAction from '@/store/auth/auth.actions';
import { Component, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { Store } from '@ngrx/store';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
@Component({
    selector: 'app-access',
    imports: [ButtonModule, RouterModule, RippleModule, FloatingConfigurator, ButtonModule],
    templateUrl: './access.html',
    styleUrl: './access.scss'
})
export class Access {
    private store = inject(Store);
    private router = inject(Router);

    reLogin() {
        this.store.dispatch(AuthAction.logout());
        this.router.navigate(['/login'], {
            replaceUrl: true
        });
    }
}
