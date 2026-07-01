import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { PRODUCT_CONFIG } from '../product-config.token';

@Component({
  selector: 'asp-app-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <div class="shell" [class]="product.themeClass">
      <header class="shell__header">
        <a class="shell__brand focus-ring" routerLink="/" [attr.aria-label]="'Open ' + product.name + ' home'">
          <span class="shell__mark">
            <img class="shell__logo" [src]="product.logoAsset" [alt]="product.logoAlt">
          </span>
          <span class="shell__brand-copy">
            <strong>{{ product.name }}</strong>
            <small>{{ product.tagline }}</small>
          </span>
        </a>
        <nav class="shell__nav" aria-label="Primary">
          @for (item of visibleNavigation; track item.route) {
            <a
              [routerLink]="item.route"
              routerLinkActive="is-active"
              [routerLinkActiveOptions]="{ exact: item.route === '/' }"
              class="focus-ring"
              [attr.aria-label]="item.ariaLabel">
              {{ item.label }}
            </a>
          }
        </nav>
      </header>
      <main class="shell__main">
        <ng-content></ng-content>
      </main>
    </div>
  `,
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent {
  readonly product = inject(PRODUCT_CONFIG);

  get visibleNavigation() {
    return this.product.navigation.filter((item) => !item.capability || this.product.capabilities[item.capability]);
  }
}
