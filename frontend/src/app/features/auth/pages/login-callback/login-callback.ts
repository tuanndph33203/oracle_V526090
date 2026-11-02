import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import AuthAction from '@/store/auth/auth.actions';

@Component({
    selector: 'app-login-callback',
    imports: [],
    templateUrl: './login-callback.html',
    styleUrl: './login-callback.scss'
})
export class LoginCallback implements OnInit {
    route = inject(ActivatedRoute);
    private store = inject(Store);

    ngOnInit() {
        const sectionId = this.route.snapshot.queryParamMap.get('sectionId');
        if (sectionId) {
            this.store.dispatch(AuthAction.callbackLogin({ sectionId }));
        } else {
            this.store.dispatch(AuthAction.failure({ error: 'Đăng nhập thất bại !' }));
            this.store.dispatch(AuthAction.logout());
        }
    }
}
