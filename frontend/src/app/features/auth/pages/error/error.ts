import { FloatingConfigurator } from '@/core/layout/floating-configurator/floating-configurator';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';

@Component({
    selector: 'app-error',
    imports: [ButtonModule, RippleModule, RouterModule, FloatingConfigurator, ButtonModule],
    templateUrl: './error.html',
    styleUrl: './error.scss'
})
export class Error {}
