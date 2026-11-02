import { Component, inject } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Password } from 'primeng/password';

@Component({
    selector: 'app-profile-change-password',
    imports: [ReactiveFormsModule, Password, Button],
    templateUrl: './profile-change-password.html',
    styleUrl: './profile-change-password.scss'
})
export class ProfileChangePassword {
    private fb = inject(FormBuilder);

    form = this.fb.group(
        {
            oldPassword: ['', Validators.required],
            newPassword: ['', [Validators.required, Validators.minLength(6)]],
            confirmPassword: ['', Validators.required]
        },
        { validators: this.matchPassword }
    );

    matchPassword(group: any) {
        const newPass = group.get('newPassword')?.value;
        const confirm = group.get('confirmPassword')?.value;
        return newPass === confirm ? null : { passwordMismatch: true };
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        console.log(this.form.value);
        // call API đổi mật khẩu ở đây
    }
}
