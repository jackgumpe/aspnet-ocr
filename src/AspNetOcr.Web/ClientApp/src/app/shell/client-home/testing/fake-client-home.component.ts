import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClientHomeIntent, ClientHomeViewModel } from '../client-home-view-model';

@Component({
  selector: 'asp-fake-client-home',
  standalone: true,
  template: `<button type="button" (click)="intent.emit({ kind: 'open_intake' })">{{ viewModel.productName }}</button>`
})
export class FakeClientHomeComponent {
  @Input({ required: true }) viewModel!: ClientHomeViewModel;
  @Output() readonly intent = new EventEmitter<ClientHomeIntent>();
}
