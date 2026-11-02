import { selectAuthState } from '@/store/auth/auth.selectors';
import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { Avatar } from 'primeng/avatar';
import { Button } from 'primeng/button';
import { DatePicker } from 'primeng/datepicker';
import { InputText } from 'primeng/inputtext';
import { RadioButton } from 'primeng/radiobutton';
import { Select } from 'primeng/select';

@Component({
    selector: 'app-profile-form',
    imports: [CommonModule, ReactiveFormsModule, InputText, Select, DatePicker, RadioButton, Avatar, Button],
    templateUrl: './profile-form.html',
    styleUrl: './profile-form.scss'
})
export class ProfileForm {
    user = signal<any>({});
    private fb = inject(FormBuilder);

    cities = [{ label: 'Hà Nội', value: 1 }];
    districts = [{ label: 'Nam Từ Liêm', value: 1 }];
    wards = [{ label: 'Mễ Trì', value: 1 }];

    genders = [
        { label: 'Nam', value: 1 },
        { label: 'Nữ', value: 2 },
        { label: 'Khác', value: 3 }
    ];

    form = this.fb.group({
        avatar: [''],
        username: ['', Validators.required],
        nickname: [''],
        email: ['', Validators.email],
        phone: [''],
        fax: [''],
        identity: [''],
        bank: [''],

        employeeCode: ['', Validators.required],
        employeeName: ['', Validators.required],
        birthday: [null],
        gender: [3],

        city: [null],
        district: [null],
        ward: [null],
        country: ['Việt Nam'],
        address: ['']
    });

    store = inject(Store);

    constructor() {
        this.store.select(selectAuthState).subscribe((user : any) => {
            this.user.set(user);
            this.form.patchValue({
                username: user?.user?.userName,
                email: user?.user?.email,
                employeeCode: user?.employees?.EmployeeCode,
                employeeName: user?.employees?.EmployeeName,
                gender: user?.employees?.Gender,
                city: user?.employees?.City,
                district: user?.employees?.District,
                ward: user?.employees?.Ward,
                address: user?.employees?.Address
            });
        });
    }   
}

