import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { finalize } from 'rxjs';
import { PreparationAreaService } from '../../../../catalogs/preparation-area/services/preparation-area';
import { PrintAgentService } from '../../services/print-agent';
import { BranchService } from '@/features/branch/services/branch';

interface PrintChoice {
    key: string;
    agentId: string;
    printerId: string;
    agentName: string;
    printerName: string;
    isAvailable: boolean;
    isOnline: boolean;
}

interface AreaPrintSetting {
    id: string;
    name: string;
    code: string;
    selectedKey: string | null;
}

interface BranchOption {
    id: string;
    name: string;
    code: string;
}

@Component({
    selector: 'app-print-settings',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, SelectModule],
    templateUrl: './print-settings.html',
    styleUrl: './print-settings.scss'
})
export class PrintSettings implements OnInit {
    readonly isLoading = signal(false);
    readonly isTesting = signal<string | null>(null);
    readonly areas = signal<AreaPrintSetting[]>([]);
    readonly printers = signal<PrintChoice[]>([]);
    readonly branches = signal<BranchOption[]>([]);
    selectedBranchId = '';

    private readonly areaService = inject(PreparationAreaService);
    private readonly agentService = inject(PrintAgentService);
    private readonly branchService = inject(BranchService);
    private readonly messages = inject(MessageService);

    ngOnInit(): void {
        this.branchService.getByFilter({ Page: 1, Size: 1000 }).subscribe({
            next: (res: any) => {
                const branches = this.readList(res);
                this.branches.set(branches
                    .filter((branch: any) => (branch.Status ?? branch.status) === 1)
                    .map((branch: any) => ({
                        id: branch.Id ?? branch.id,
                        name: branch.Name ?? branch.name ?? 'Chi nhánh',
                        code: branch.Code ?? branch.code ?? ''
                    })));
                if (this.branches().length === 1) {
                    this.selectedBranchId = this.branches()[0].id;
                    this.load();
                }
            },
            error: () => this.messages.add({ severity: 'error', summary: 'Không tải được danh sách chi nhánh' })
        });
    }

    load(): void {
        if (!this.selectedBranchId) return;
        this.isLoading.set(true);
        let areas: any[] = [];
        let agents: any[] = [];
        let mappings: any[] = [];
        let pending = 3;
        const finish = () => {
            pending -= 1;
            if (pending > 0) return;
            const printers = agents.flatMap((agent) => (agent.Printers ?? agent.printers ?? []).map((printer: any) => ({
                key: `${agent.Id ?? agent.id}|${printer.Id ?? printer.id}`,
                agentId: agent.Id ?? agent.id,
                printerId: printer.Id ?? printer.id,
                agentName: agent.MachineName ?? agent.machineName ?? 'Máy tính',
                printerName: printer.PrinterName ?? printer.printerName,
                isAvailable: printer.IsAvailable ?? printer.isAvailable ?? false,
                isOnline: agent.IsOnline ?? agent.isOnline ?? false
            } as PrintChoice))).sort((left, right) => Number(this.isVirtualPrinter(left.printerName)) - Number(this.isVirtualPrinter(right.printerName)) || left.printerName.localeCompare(right.printerName));
            this.printers.set(printers);
            const mappingByArea = new Map(mappings.map((mapping) => [mapping.PreparationAreaId ?? mapping.preparationAreaId, mapping]));
            this.areas.set(areas.map((area) => {
                const id = area.Id ?? area.id;
                const mapping = mappingByArea.get(id);
                const key = mapping ? `${mapping.AgentId ?? mapping.agentId}|${mapping.PrinterId ?? mapping.printerId}` : null;
                return { id, name: area.Name ?? area.name ?? 'Khu vực', code: area.Code ?? area.code ?? '', selectedKey: printers.some((printer) => printer.key === key) ? key : null };
            }));
            if (!areas.length) {
                console.info('[PrintSettings] API không trả khu vực cho chi nhánh đã chọn.', { branchId: this.selectedBranchId });
            }
            this.isLoading.set(false);
        };

        this.areaService.getByFilter({ Page: 1, Size: 1000, BranchId: this.selectedBranchId }).subscribe({
            next: (res: any) => {
                areas = this.readList(res).filter((area: any) => (area.Status ?? area.status) === 1);
                const selectedBranchId = this.selectedBranchId.toLowerCase();
                const areasAssignedToOtherBranches = areas.filter((area: any) => {
                    const branchIds = area.BranchIds ?? area.branchIds;
                    return Array.isArray(branchIds) && branchIds.length > 0 && !branchIds.some((id: string) => id.toLowerCase() === selectedBranchId);
                });
                if (areas.length > 0 && areasAssignedToOtherBranches.length === areas.length) {
                    console.warn('[PrintSettings] Các khu vực đang gắn với chi nhánh khác.', {
                        selectedBranchId: this.selectedBranchId,
                        areaBranchIds: areas.map((area: any) => ({ code: area.Code ?? area.code, branchIds: area.BranchIds ?? area.branchIds }))
                    });
                }
                finish();
            },
            error: () => {
                this.messages.add({ severity: 'error', summary: 'Không tải được khu vực chế biến' });
                finish();
            }
        });
        this.agentService.getAgents().subscribe({
            next: (res: any) => {
                agents = (Array.isArray(res) ? res : res?.Data ?? []).filter((agent: any) => (agent.IsOnline ?? agent.isOnline) && String(agent.BranchId ?? agent.branchId).toLowerCase() === this.selectedBranchId.toLowerCase());
                finish();
            },
            error: () => {
                this.messages.add({ severity: 'error', summary: 'Không tải được danh sách máy in', detail: 'Vui lòng kiểm tra kết nối mạng rồi thử lại.' });
                finish();
            }
        });
        this.agentService.getAreaMappings(this.selectedBranchId).subscribe({
            next: (res: any) => { mappings = Array.isArray(res) ? res : res?.Data ?? []; finish(); },
            error: () => { this.messages.add({ severity: 'error', summary: 'Không tải được cấu hình máy in đã lưu' }); finish(); }
        });
    }

    choicesForArea(): PrintChoice[] {
        return this.printers().filter((printer) => printer.isOnline && printer.isAvailable && !this.isVirtualPrinter(printer.printerName));
    }

    saveMappings(): void {
        if (!this.selectedBranchId) return;
        const payload = {
            BranchId: this.selectedBranchId,
            Items: this.areas().filter((area) => area.selectedKey).map((area) => {
                const [AgentId, PrinterId] = area.selectedKey!.split('|');
                return { PreparationAreaId: area.id, AgentId, PrinterId };
            })
        };
        this.isLoading.set(true);
        this.agentService.saveAreaMappings(payload).pipe(finalize(() => this.isLoading.set(false))).subscribe({
            next: () => this.messages.add({ severity: 'success', summary: 'Đã lưu cấu hình máy in', detail: 'Phiếu Bếp/Bar sẽ được gửi đến máy in đã chọn.' }),
            error: (error: any) => this.messages.add({ severity: 'error', summary: 'Không lưu được cấu hình', detail: error?.error?.Message || error?.error?.message || 'Vui lòng kiểm tra chi nhánh và kết nối máy in.' })
        });
    }

    testPrint(area: AreaPrintSetting): void {
        const choice = this.printers().find((printer) => printer.key === area.selectedKey);
        if (!choice) return;
        this.isTesting.set(area.id);
        this.agentService.printTest(choice.agentId, choice.printerId).pipe(finalize(() => this.isTesting.set(null))).subscribe({
            next: () => this.messages.add({ severity: 'success', summary: 'Đã gửi lệnh in thử', detail: `${area.name} → ${choice.printerName}` }),
            error: (error: any) => this.messages.add({ severity: 'error', summary: 'Không gửi được lệnh in thử', detail: error?.error?.Message || error?.error?.message || 'Vui lòng kiểm tra máy tính kết nối và máy in.' })
        });
    }

    trackPrinter(_: number, printer: PrintChoice): string { return printer.key; }

    private readList(response: any): any[] {
        const data = response?.Data ?? response?.data;
        const content = data?.Content ?? data?.content ?? data?.Items ?? data?.items;
        if (Array.isArray(content)) return content;
        if (Array.isArray(data)) return data;
        return [];
    }

    private isVirtualPrinter(name: string): boolean {
        return /\b(pdf|xps|fax|onenote|wps)\b/i.test(name);
    }

}
