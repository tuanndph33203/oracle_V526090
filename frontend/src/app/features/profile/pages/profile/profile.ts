import { Component, signal } from '@angular/core';

import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TabsModule } from 'primeng/tabs';
import { tabsConfig } from '../../constants/profile';
import { Listbox } from 'primeng/listbox';
import { FormsModule } from '@angular/forms';
import { ProfileForm } from '../../components/profile-form/profile-form';
import { ProfileChangePassword } from '../../components/profile-change-password/profile-change-password';

@Component({
    selector: 'app-profile',
    standalone: true,
    imports: [FormsModule, InputTextModule, ButtonModule, TabsModule, Listbox, ProfileForm, ProfileChangePassword],
    templateUrl: './profile.html',
    styleUrl: './profile.scss'
})
export class Profile {
    tabs = signal<any[]>([]);
    value: number = 0;

    constructor() {
        this.tabs.set(tabsConfig);
    }
}
