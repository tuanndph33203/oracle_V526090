import { Component, input } from '@angular/core';
import { ButtonModule, ButtonSeverity } from 'primeng/button';
import { CommonModule } from '@angular/common';
import { DrawerModule } from 'primeng/drawer';
import { StyleClassModule } from 'primeng/styleclass';
import { Configurator } from '../configurator/configurator';

@Component({
    selector: 'app-floating-configurator',
    imports: [CommonModule, ButtonModule, StyleClassModule, Configurator, DrawerModule],
    templateUrl: './floating-configurator.html',
    styleUrl: './floating-configurator.scss'
})
export class FloatingConfigurator {
    visible: boolean = false;

    rounded = input<boolean>(false);
    severity = input<ButtonSeverity>('primary');
    raised = input<boolean>(false);

    float = input<boolean>(true);
}
