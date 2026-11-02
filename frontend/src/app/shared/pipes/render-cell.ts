import { ITableConfig } from '@/core/models/common';
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'renderCell',
  standalone: true
})
export class RenderCellPipe implements PipeTransform {

  transform(item: ITableConfig, value: any, row: any): string {

    if (item.transformFn) {
      return item.transformFn(value);
    }

    if (item.transformRowFn) {
      return item.transformRowFn(row);
    }

    return value ?? '';
  }

}