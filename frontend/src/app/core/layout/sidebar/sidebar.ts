import { Component, ElementRef, inject, OnInit, signal, OnDestroy, ViewChild } from '@angular/core';
import { Menu } from '../menu/menu';
import { LayoutService } from '@/core/services/layout';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PopoverModule, Popover } from 'primeng/popover';
import { Listbox, ListboxSelectAllChangeEvent } from 'primeng/listbox';
import { ButtonModule } from 'primeng/button';
import { Store, select } from '@ngrx/store';
import { Subject, takeUntil } from 'rxjs';
import { selectAvailableBranchCodes, selectSelectedBranchCodes } from '@/store/branch-filter/branch-filter.selectors';
import BranchFilterAction from '@/store/branch-filter/branch-filter.actions';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-sidebar',
    imports: [Menu, CommonModule, FormsModule, PopoverModule, Listbox, ButtonModule],
    templateUrl: './sidebar.html',
    styleUrl: './sidebar.scss'
})
export class Sidebar implements OnInit, OnDestroy {
    @ViewChild('opSidebar') opSidebar!: Popover;

    layoutService = inject(LayoutService);
    store = inject(Store);
    
    name = environment.name;
    branchOptions = signal<any[]>([]);
    availableBranchCodes: any[] = [];
    selectedBranchCodes: string[] = [];
    branchUser: any[] = [];
    selectAll: boolean = true;
    
    private destroy = new Subject<void>();

    constructor(public el: ElementRef) {}

    ngOnInit(): void {
        this.store.pipe(select(selectAvailableBranchCodes), takeUntil(this.destroy)).subscribe((options) => {
            if (!options.length) return;
            this.branchOptions.set(options);
            this.applyBranch();
        });

        this.store.pipe(select(selectSelectedBranchCodes), takeUntil(this.destroy)).subscribe((selectedCodes) => {
            this.selectedBranchCodes = selectedCodes.map((code: any) => code.Code) ?? [];
            this.applyBranch();
        });
    }

    applyBranch() {
        const selectedCodes = this.selectedBranchCodes ?? [];
        this.branchUser = this.branchOptions().filter((b: any) => selectedCodes.includes(b.Code));
    }

    applyFilter() {
        const branchCodes = this.branchUser ?? [];
        if (!branchCodes.length) return;
        this.store.dispatch(BranchFilterAction.setSelectedBranches({ codes: branchCodes }));
    }

    onChange() {
        this.applyFilter();
    }

    onSelectAllChange(event: ListboxSelectAllChangeEvent) {
        this.branchUser = event.checked ? [...this.branchOptions()] : [this.branchOptions()[0]];
        this.selectAll = event.checked;
        this.applyFilter();
    }

    ngOnDestroy(): void {
        this.opSidebar?.hide();
        this.destroy.next();
        this.destroy.complete();
    }
}
