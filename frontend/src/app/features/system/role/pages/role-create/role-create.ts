import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TreeNode } from 'primeng/api';
import { AutoComplete } from 'primeng/autocomplete';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { TreeTableModule } from 'primeng/treetable';
import { ActivatedRoute, Router } from '@angular/router';
import { Tree } from 'primeng/tree';
import { forkJoin } from 'rxjs';
import { PermissionService } from '../../../navigation/services/permission';
import { RoleService } from '../../services/role';

@Component({
    selector: 'app-role-create',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, ButtonModule, TextareaModule, TreeTableModule, Tree, AutoComplete],
    templateUrl: './role-create.html',
    styleUrl: './role-create.scss'
})
export class RoleCreate extends BaseFormPage<any, any> implements OnInit {
    edition = signal(false);
    form: FormGroup;
    permissionsTree = signal<TreeNode[]>([]);
    filteredPermissions = signal<any[]>([]);
    selectionKeys: TreeNode[] | Record<string, TreeNode> = [];

    roles = signal<any[]>([]);

    roleService = inject(RoleService);
    private permissionService = inject(PermissionService);
    private activate = inject(ActivatedRoute);
    private router = inject(Router);

    constructor() {
        super();
        this.form = this.fb.group({
            code: ['', Validators.required],
            name: ['', Validators.required],
            description: ['']
        });
    }

    ngOnInit(): void {
        this.activate.params.subscribe((params) => {
            if (params['id']) {
                this.edition.set(true);
                forkJoin({
                    role: this.roleService.getById(params['id']),
                    permissions: this.permissionService.getByFilter({ isTree: true })
                }).subscribe(({ role, permissions }) => {
                    this.form.patchValue({
                        code: role.data.code,
                        name: role.data.name,
                        description: role.data.description
                    });
                    const nodes = this.mapPermissionsToTree(permissions.data);
                    this.permissionsTree.set(nodes);
                    this.selectionKeys = this.mapSelectedNodes(nodes, role.data.permissionIds);
                });
            }
        });
        this.searchPermission();
    }

    getSelectedNodes(ctx: TreeNode[] | Record<string, TreeNode> | null | undefined): TreeNode[] {
        if (!ctx) return [];

        const items = Array.isArray(ctx) ? ctx : Object.values(ctx);
        return items.filter((item: TreeNode | undefined) => !!item?.data) as TreeNode[];
    }

    searchPermission(): void {
        this.permissionService.getByFilter({ isTree: true }).subscribe((res) => {
            console.log(res);

            const nodes = this.mapPermissionsToTree(res.data);
            this.permissionsTree.set(nodes);
        });
    }

    submit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        const payload = this.form.value;
        const selectedIds = this.getSelectedNodes(this.selectionKeys)
            .filter((n) => n.data.id)
            .map((n) => n.data.id);

        if (!selectedIds.length) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Vui lòng chọn ít nhất 1 quyền'
            });
            return;
        }

        payload.permissionIds = selectedIds;

        const request$ = this.edition() ? this.roleService.update({ ...payload, id: this.activate.snapshot.params['id'] }) : this.roleService.create(payload);
        request$.subscribe(() => this.onSuccess());
    }

    mapPermissionsToTree(data: any[]): any[] {
        return data.map((group, groupIndex) => ({
            key: `group-${groupIndex}`,
            label: group.name || 'OTHER',
            data: group.id ?? null,
            expanded: true,
            children: (group.items || []).map((item: any) => ({
                key: item.id,
                label: item.name,
                data: item
            }))
        }));
    }

    mapSelectedNodes(tree: TreeNode[], selectedIds = [] as string[]): TreeNode[] {
        const result: TreeNode[] = [];
        const traverse = (nodes: TreeNode[]) => {
            nodes.forEach((node) => {
                if (!node.children && selectedIds.includes(node.key as string)) {
                    result.push(node);
                }
                if (node.children) {
                    traverse(node.children);
                }
            });
        };
        traverse(tree);
        return result;
    }

    trackByNode(index: number, node: any) {
        return node.key;
    }

    goBack() {
        this.router.navigate(['/systems/role']);
    }

    private onSuccess() {
        this.messageService.add({
            severity: 'info',
            summary: 'Thành công!'
        });

        this.router.navigate(['/systems/role']);
    }
}
