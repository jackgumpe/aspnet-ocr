import { Component } from '@angular/core';
import { PanelComponent } from '../../ui/panel/panel.component';

@Component({
  selector: 'asp-client-inventory-placeholder',
  standalone: true,
  imports: [PanelComponent],
  template: `
    <section class="page-head">
      <p class="eyebrow">Inventory</p>
      <h1>Client inventory</h1>
    </section>

    <asp-panel title="Schema" eyebrow="Placeholder">
      <div class="schema-grid" aria-label="Client inventory schema">
        <span>clientId</span>
        <span>displayName</span>
        <span>defaultProvider</span>
        <span>retentionPolicy</span>
        <span>createdAtUtc</span>
      </div>
    </asp-panel>
  `,
  styleUrl: './client-inventory-placeholder.component.scss'
})
export class ClientInventoryPlaceholderComponent {}
