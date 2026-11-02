import { Component, input } from '@angular/core';
import { ProgressSpinner } from 'primeng/progressspinner';

@Component({
  selector: 'app-loading',
  imports: [ProgressSpinner],
  templateUrl: './loading.html',
  styleUrl: './loading.scss',
})
export class Loading {
  wrapperClass = input<string>(
    'absolute inset-0 flex flex-col items-center justify-center text-gray-400 gap-2',
  );

  styleClass = input<string>('');
  strokeWidth = input<string>('2');
  fill = input<string>('none');
  animationDuration = input<string>('2s');
  ariaLabel = input<string>('Loading...');
}
