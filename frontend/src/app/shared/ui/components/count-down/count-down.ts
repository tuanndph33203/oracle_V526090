import { ClockService } from '@/core/services/clock';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { Badge } from 'primeng/badge';

@Component({
  selector: 'app-count-down',
  standalone: true,
  imports: [Badge],
  templateUrl: './count-down.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CountDown {
  targetDate = input<Date | null | undefined>();
  severity = input<'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'>();
  badgeSize = input<'small' | 'large' | 'xlarge'>();
  styleClass = input<string>();

  onEndCount = output();

  private clock = inject(ClockService);

  time = computed(() => {
    const target = this.targetDate();
    const now = this.clock.time();

    if (!target) return '00:00:00';

    const diff = target.getTime() - now;

    if (diff <= 0) {
      queueMicrotask(() => this.onEndCount.emit());
      return '00:00:00';
    }

    const totalSeconds = Math.floor(diff / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    return `${this.pad(hours)}:${this.pad(minutes)}:${this.pad(seconds)}`;
  });

  private pad(v: number) {
    return v.toString().padStart(2, '0');
  }
}