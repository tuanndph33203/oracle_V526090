import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { NavigationService } from '../../services/navigation';
import { AutoComplete, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { cleanObject } from '@/core/utils/cleanQuery';
import { PermissionService } from '../../services/permission';
import { InputNumber } from 'primeng/inputnumber';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { filterAllIcons, allIcons, primeIcons } from '@/core/constants/icon';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { AutoCompleteSearchDirective } from "@/shared/directive/autocompleteSearch";

@Component({
    selector: 'app-navigation-create',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, ButtonModule, AutoComplete, InputNumber, ToggleSwitch, AutoComplete, DialogWrap, AutoCompleteSearchDirective],
    templateUrl: './navigation-create.html',
    styleUrl: './navigation-create.scss'
})
export class NavigationCreate extends BaseFormPage<any, any> implements OnInit {
    form: FormGroup;

    firstLoad = true;
    edition = signal(false);
    searchQuery = signal<any>({
        TextSearch: ''
    });

    filteredNavs = signal<any[]>([]);
    permissions = signal<any[]>([]);
    navigations = signal<any[]>([]);
    filteredPermissions = signal<any[]>([]);
    filteredIcon = signal<any[]>([]);
    navigationService = inject(NavigationService);
    permissionService = inject(PermissionService);

    constructor() {
        super();
        this.form = this.fb.group({
            name: ['', [Validators.required, Validators.maxLength(255)]],
            icon: [{ key: 'CIRCLE', class: 'pi pi-circle' }],
            path: ['', [Validators.maxLength(255)]],
            parentId: [null],
            order: [0, [Validators.required, Validators.min(0)]],
            status: [true],
            permissionIds: [null, [Validators.required, Validators.minLength(1)]]
        });
    }

    ngOnInit(): void {
        this.permissionService
            .getByFilter({ isView: true })
            .pipe()
            .subscribe((res: any) => {
                const items = this.getResponseItems(res);
                this.permissions.set(items);
                this.filteredPermissions.set(items);
            });

        this.navigationService
            .getByFilter({})
            .pipe()
            .subscribe((res: any) => {
                const items = this.getResponseItems(res);
                this.navigations.set(items);
                this.filteredNavs.set(items);
                let parent;
                if (data?.edition) {
                    parent = this.navigations().find((item) => item.id === data?.data.parentId);
                } else {
                    parent = data?.data.parentId;
                }

                this.form.patchValue({
                    parentId: parent
                });
            });

        const data = this.config?.data;

        if (data?.edition && data?.data) {
            this.edition.set(true);
            const rawIcon = data.data.icon || '';
            const iconObj = allIcons.find((i) => i.class === rawIcon || i.key.toLowerCase() === rawIcon.toLowerCase());
            this.form.patchValue({
                name: data.data.lable,
                path: data.data.routerLink,
                icon: iconObj ?? (rawIcon ? { key: rawIcon, class: rawIcon } : null),
                permissionIds: data.data.listPermission,
                parentId: data.data.parentId ?? null,
                order: data.data.order,
                status: data.data.status
            });
        }
    }

    submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const iconValue = this.form.value?.icon;
        let iconClass = '';
        if (typeof iconValue === 'string') {
            const found = allIcons.find((i) => i.key.toLowerCase() === iconValue.toLowerCase() || i.class === iconValue);
            iconClass = found ? found.class : iconValue;
        } else if (iconValue && typeof iconValue === 'object') {
            iconClass = iconValue.class || '';
        }

        const payload = {
            ...(this.edition() ? { id: this.config?.data.data.id } : {}),
            ...this.form.value,
            icon: iconClass,
            parentId: this.form.value?.parentId?.id ?? null,
            permissionIds: this.form.value?.permissionIds.map((item: any) => item.id)
        };

        const request$ = this.edition() ? this.navigationService.update(payload) : this.navigationService.create(payload);

        request$.subscribe((res) => {
            if (res.code === 200) {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Thành công'
                });
                this.dialogRef?.close(true);
            }

            if (res.code === 409) {
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Tên hoặc path đã tồn tại'
                });
            }
        });
    }

    normalize = (v: string) =>
        v
            .toLowerCase()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '');

    searchComplete(event: any, service: any, target: any) {
        const query = this.normalize(event?.query || '');

        target.set(service().filter((item: any) => this.normalize(item.name ?? '').includes(query)));
    }

    onFilterIcon(event: { query: string }) {
        this.filteredIcon.set(filterAllIcons(event.query));
    }
    getIconByClass(cls: string) {
        return allIcons.find((i) => i.class === cls);
    }
}
