import { FloatingConfigurator } from '@/core/layout/floating-configurator/floating-configurator';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Button } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { RippleModule } from 'primeng/ripple';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import AuthAction from '@/store/auth/auth.actions';
@Component({
    selector: 'app-login',
    imports: [CommonModule, CheckboxModule, InputTextModule, PasswordModule, RouterModule, RippleModule, FloatingConfigurator, Button, ReactiveFormsModule],
    templateUrl: './login.html',
    styleUrl: './login.scss'
})
export class Login {
    form!: FormGroup;
    email: string = '';

    password: string = '';

    checked: boolean = false;
    isInternalLogin: boolean = true;
    authService = inject(AuthService);
    store = inject(Store);
    constructor(private fb: FormBuilder) {
        this.form = this.fb.group({
            email: ['admin@utc.edu.vn', [Validators.required, Validators.email]],
            password: ['CafeAdmin123#', [Validators.required, Validators.minLength(6)]]
        });
    }

    onLoginWidthMirosoft(): void {
        this.authService.redirectMicrosoft();
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const { email, password } = this.form.value;
        this.store.dispatch(AuthAction.requestLogin({ email, password }));
    }
}
