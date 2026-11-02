import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AutoComplete } from 'primeng/autocomplete';
import { ButtonModule } from 'primeng/button';
import { EditorModule } from 'primeng/editor';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { catchError, map, of, switchMap } from 'rxjs';
import { MinioService } from '@/core/services/minio';
import { arrayToString } from '@/core/utils/arrayToString';
import { roundBank } from '@/core/utils/roudBank';
import { BaseFormPage } from '@/shared/base/base-form-page';
import { AutoCompleteSearchDirective } from '@/shared/directive/autocompleteSearch';
import { FileUploadMeta } from '@/shared/ui/form/file-upload-meta/file-upload-meta';
import { DialogWrap } from '@/shared/ui/layout/dialog-wrap/dialog-wrap';
import { TaxRateService } from '@/features/catalogs/tax-rate/services/tax-rate';
import { BranchService } from '@/features/branch/services/branch';
import { ProductGroupService } from '@/features/catalogs/product-group/services/product-group';
import { ProductTypeService } from '@/features/catalogs/product-type/services/product-type';
import { UnitService } from '@/features/catalogs/unit/services/unit';
import { ProductService } from '../../services/product';

@Component({
    selector: 'app-product-create',
    imports: [CommonModule, ReactiveFormsModule, InputTextModule, InputNumberModule, AutoComplete, ButtonModule, EditorModule, FileUploadMeta, DialogWrap, Textarea, AutoCompleteSearchDirective],
    templateUrl: './product-create.html',
    styleUrl: './product-create.scss'
})
export class ProductCreate extends BaseFormPage<any, any> implements OnInit {
    imageFile = signal<File[] | null>(null);
    edition = signal(false);
    form: FormGroup;
    unitOptions = signal<any[]>([]);
    productTypeOptions = signal<any[]>([]);
    productGroupOptions = signal<any[]>([]);
    branchOptions = signal<any[]>([]);
    branchMultiOptions = signal<any[]>([]);
    vatOptions = signal<any[]>([]);
    serviceService = inject(ProductService);

    unitService = inject(UnitService);
    vatService = inject(TaxRateService);
    productTypeService = inject(ProductTypeService);
    productGroupService = inject(ProductGroupService);
    branchService = inject(BranchService);

    private minioService = inject(MinioService);

    constructor() {
        super();
        this.form = this.fb.group({
            Code: ['', [Validators.required, Validators.maxLength(20)]],
            Name: ['', [Validators.required, Validators.maxLength(255)]],
            UnitCode: [null, Validators.required],
            ProductTypeCode: [null],
            ProductGroupCode: [null],
            BranchCode: [null, Validators.required],
            BarCode: ['', [Validators.maxLength(50)]],
            Origin: ['', [Validators.maxLength(100)]],
            PurchasePrice: [0, [Validators.min(0)]],
            SellingPrice: [0, [Validators.required, Validators.min(0)]],
            ImportTaxCode: [null],
            SpecialTaxCode: [null],
            ExportTaxCode: [null],
            ExportTaxMoney: [0, [Validators.min(0)]],
            Notes: [''],
            Description: [''],
            ApplyListBranchCodes: [[]],
            Status: [1],
            AvatarUrl: ['']
        });
    }

    ngOnInit(): void {
        this.searchUnit({ query: '' });
        this.searchProductType({ query: '' });
        this.searchProductGroup({ query: '' });
        this.searchBranch({ query: '' });
        this.searchVat({ query: '' });

        if (this.config?.data?.edition) {
            this.edition.set(true);
            const data = this.config?.data?.data;
            const latestPriceItem = this.getLatestPriceItem(data);

            this.form.patchValue({
                Code: data.Code,
                Name: data.Name,
                UnitCode: data.Units,
                ProductTypeCode: data.ProductType,
                ProductGroupCode: data.ProductGroup,
                BranchCode: data.Branch,
                BarCode: data.BarCode,
                Origin: data.Origin,
                PurchasePrice: data.PurchasePrice,
                SellingPrice: latestPriceItem?.SellingPrice ?? data.SellingPrice ?? 0,
                ImportTaxCode: data.ImportTax,
                SpecialTaxCode: data.SpecialTax,
                ExportTaxCode: latestPriceItem?.ExportTaxCode
                    ? {
                          Code: latestPriceItem.ExportTaxCode,
                          Name: latestPriceItem.ExportTaxCode,
                          VatNumber: latestPriceItem.ExportTaxNumber ?? 0
                      }
                    : null,
                ExportTaxMoney: latestPriceItem?.ExportTaxMoney ?? data.ExportTaxMoney ?? 0,
                Notes: data.Notes,
                Description: data.Description,
                ApplyListBranchCodes: data.ApplyListBranchCodes ?? [],
                Status: data.Status,
                AvatarUrl: data.AvatarUrl
            });
            return;
        }

        this.form.patchValue({
            BranchCode: this.branchUser()[0] ?? null
        });
    }

    submit() {
        if (!this.validateForm()) {
            return;
        }

        const file = this.imageFile();
        const upload$ = file
            ? this.minioService.upload(file[0]).pipe(
                  map((res) => res?.Data?.ObjectName),
                  catchError(() => {
                      this.messageService.add({
                          severity: 'warn',
                          summary: 'Tai anh that bai, vui long thu lai'
                      });
                      return of(undefined);
                  })
              )
            : of(undefined);

        upload$
            .pipe(
                switchMap((image) => {
                    const payload = {
                        ...(this.edition() ? { Id: this.config?.data.data.Id } : {}),
                        ...this.form.value,
                        ImportTaxCode: this.form.value.ImportTaxCode?.Code ?? '',
                        SpecialTaxCode: this.form.value.SpecialTaxCode?.Code ?? '',
                        AvatarUrl: image ? image : this.form.value.AvatarUrl ?? '',
                        UnitCode: this.form.value.UnitCode?.Code ?? '',
                        ProductTypeCode: this.form.value?.ProductTypeCode?.Code ?? '',
                        ProductGroupCode: this.form.value?.ProductGroupCode?.Code ?? '',
                        BranchCode: this.form.value?.BranchCode?.Code ?? '',
                        ApplyListBranchCodes: arrayToString((this.form.value?.ApplyListBranchCodes ?? []).map((item: any) => item?.Code ?? '')),
                        ListItemsMoneyTerm: [
                            {
                                SellingPrice: this.form.value.SellingPrice ?? 0,
                                ExportTaxMoney: this.form.value.ExportTaxMoney ?? 0,
                                ExportTaxNumber: this.form.value.ExportTaxCode?.VatNumber ?? 0,
                                ExportTaxCode: this.form.value.ExportTaxCode?.Code ?? ''
                            }
                        ]
                    };

                    return this.edition() ? this.serviceService.update(payload) : this.serviceService.create(payload);
                })
            )
            .subscribe((res) => {
                this.handleResponse(res);
            });
    }

    searchUnit(event: any) {
        const query = this.buildQuery(event.query);
        this.unitService.getByFilter(query).subscribe((res) => {
            this.unitOptions.set(res?.Data?.Content || []);
        });
    }

    searchProductType(event: any) {
        const query = this.buildQuery(event.query);
        this.productTypeService.getByFilter(query).subscribe((res) => {
            this.productTypeOptions.set(res?.Data?.Content || []);
        });
    }

    searchProductGroup(event: any) {
        const query = this.buildQuery(event.query);
        this.productGroupService.getByFilter(query).subscribe((res) => {
            this.productGroupOptions.set(res?.Data?.Content || []);
        });
    }

    searchBranch(event: any) {
        const query = this.buildQuery(event.query);
        this.branchService.getByFilter(query).subscribe((res) => {
            const items = res?.Data?.Content || [];
            this.branchOptions.set(items);
            this.branchMultiOptions.set(items);
        });
    }

    searchVat(event: any) {
        const query = this.buildQuery(event.query);
        this.vatService.getByFilter(query).subscribe((res) => {
            this.vatOptions.set(res?.Data?.Content || []);
        });
    }

    onImagesUploaded(files: File[]) {
        this.imageFile.set(files);
    }

    getTotalPrice(): number {
        const sellingPrice = +this.form.get('SellingPrice')?.value || 0;
        const exportTaxMoney = +this.form.get('ExportTaxMoney')?.value || 0;
        const exportTaxPercent = +this.form.get('ExportTaxCode')?.value?.VatNumber || 0;
        const tax = exportTaxMoney > 0 ? exportTaxMoney : (sellingPrice * exportTaxPercent) / 100;
        return roundBank(sellingPrice + tax);
    }

    private buildQuery(query?: string) {
        return {
            page: 1,
            size: 15,
            TextSearch: query || ''
        };
    }

    private getLatestPriceItem(data: any) {
        const priceItems = data?.ProductMoneyTermRelas ?? [];
        if (!priceItems.length) return null;

        return priceItems.reduce((latest: any, current: any) => {
            if (!latest) return current;
            return new Date(current.StartDate) > new Date(latest.StartDate) ? current : latest;
        }, null);
    }
}
