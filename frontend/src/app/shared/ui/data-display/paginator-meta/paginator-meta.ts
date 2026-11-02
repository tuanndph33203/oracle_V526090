import { Component, computed, effect, input, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PaginatorState } from 'primeng/paginator';
import { Select } from 'primeng/select';

@Component({
    selector: 'app-paginator-meta',
    imports: [Select, FormsModule],
    templateUrl: './paginator-meta.html',
    styleUrl: './paginator-meta.scss'
})
export class PaginatorMeta {
    optionPanigations = [10, 15, 50, 100, 200, 500, 1000];
    totalRecords = input<number>(0);
    rows = input<number>(10);
    currentPage = input<number>(1);
    showTotal = input<boolean>(true);
    showSize = input<boolean>(true);
    showNavigation = input<boolean>(true);

    selectedSize = signal(10);

    onPageChange = output<PaginatorState>();

    first = signal(0);

    totalPages = computed(() => Math.max(1, Math.ceil(this.totalRecords() / this.rows())));

    currentPageDisplay = computed(() => Math.floor(this.first() / this.rows()) + 1);

    pages = computed<(number | '...')[]>(() => {
        const total = this.totalPages();
        const current = this.currentPageDisplay();

        if (total <= 5) {
            return Array.from({ length: total }, (_, i) => i + 1);
        }

        const result: (number | '...')[] = [];

        let start = current - 2;
        let end = current + 2;

        if (start < 1) {
            start = 1;
            end = 5;
        }

        if (end > total) {
            end = total;
            start = total - 4;
        }

        for (let i = start; i <= end; i++) {
            result.push(i);
        }

        if (end < total - 1) {
            result.push('...');
        }

        if (end < total) {
            result.push(total);
        }
        return result;
    });
    isFirstPage = computed(() => this.currentPageDisplay() === 1);

    isLastPage = computed(() => this.currentPageDisplay() === this.totalPages());
    constructor() {
        effect(() => {
            this.selectedSize.set(this.rows());
        });

        effect(() => {
            const page = Math.max(this.currentPage(), 1);
            const rows = Math.max(this.rows(), 1);
            const nextFirst = (page - 1) * rows;
            if (untracked(() => this.first()) !== nextFirst) {
                this.first.set(nextFirst);
            }
        });
    }
    goFirst() {
        if (this.currentPageDisplay() === 1) return;

        this.first.set(0);
        this.emit();
    }
    goLast() {
        const lastFirst = (this.totalPages() - 1) * this.rows();
        if (this.first() === lastFirst) return;

        this.first.set(lastFirst);
        this.emit();
    }

    goTo(page: number) {
        this.first.set((page - 1) * this.rows());
        this.emit();
    }

    next() {
        if (this.currentPageDisplay() < this.totalPages()) {
            this.first.update((v) => v + this.rows());
            this.emit();
        }
    }

    prev() {
        if (this.currentPageDisplay() > 1) {
            this.first.update((v) => v - this.rows());
            this.emit();
        }
    }

    onChange() {
        this.first.set(0);
        this.onPageChange.emit({
            page: 0,
            first: 0,
            rows: this.selectedSize(),
            pageCount: this.totalPages()
        });
    }

    private emit() {
        this.onPageChange.emit({
            page: this.currentPageDisplay() - 1,
            first: (this.currentPageDisplay() - 1) * this.rows(),
            rows: this.rows(),
            pageCount: this.totalPages()
        });
    }
}
