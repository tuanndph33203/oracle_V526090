import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ClockService {
  private now = signal(Date.now());
  readonly time = this.now.asReadonly();

  constructor() {
    setInterval(() => {
      this.now.set(Date.now());
    }, 1000);
  }
}