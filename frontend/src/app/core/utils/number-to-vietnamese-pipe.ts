import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'numberToVietnamese',
})
export class NumberToVietnamesePipe implements PipeTransform {
  private readonly chuSo = ['không', 'một', 'hai', 'ba', 'bốn', 'năm', 'sáu', 'bảy', 'tám', 'chín'];

  private readonly hang = ['', 'nghìn', 'triệu', 'tỷ', 'nghìn tỷ', 'triệu tỷ', 'tỷ tỷ'];

  private doc3So(num: number): string {
    let tram = Math.floor(num / 100);
    let chuc = Math.floor((num % 100) / 10);
    let donvi = num % 10;
    let str = '';

    if (tram > 0) {
      str += this.chuSo[tram] + ' trăm';
      if (chuc === 0 && donvi > 0) str += ' linh';
    }

    if (chuc > 1) {
      str += ' ' + this.chuSo[chuc] + ' mươi';
      if (chuc > 1 && donvi === 1) str += ' mốt';
    } else if (chuc === 1) {
      str += ' mười';
      if (donvi === 1) str += ' một';
    }

    if (chuc > 1 && donvi === 5) {
      str += ' lăm';
    } else if (donvi > 0 && !(chuc > 0 && donvi === 1)) {
      str += ' ' + this.chuSo[donvi];
    }

    return str.trim();
  }

  transform(value: number | string | null | undefined): string {
    if (value == null || Number(value) < 0) return '';

    if (value === 0) return 'Không';

    let str = '';
    let i = 0;

    while (Number(value) > 0) {
      const block = Number(value) % 1000;

      if (block !== 0) {
        const prefix = this.doc3So(block);
        str = `${prefix} ${this.hang[i]} ${str}`.trim();
      }

      value = Math.floor(Number(value) / 1000);
      i++;
    }

    return str.charAt(0).toUpperCase() + str.slice(1);
  }
}
