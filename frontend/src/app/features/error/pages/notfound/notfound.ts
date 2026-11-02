import { FloatingConfigurator } from '@/core/layout/floating-configurator/floating-configurator';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-notfound',
    imports: [RouterModule, FloatingConfigurator, ButtonModule],
    templateUrl: './notfound.html',
    styleUrl: './notfound.scss'
})
export class Notfound {}
