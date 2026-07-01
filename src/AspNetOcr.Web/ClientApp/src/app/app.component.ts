import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppShellComponent } from './shell/app-shell/app-shell.component';

@Component({
  selector: 'asp-root',
  standalone: true,
  imports: [AppShellComponent, RouterOutlet],
  template: `
    <asp-app-shell>
      <router-outlet></router-outlet>
    </asp-app-shell>
  `
})
export class AppComponent {}
