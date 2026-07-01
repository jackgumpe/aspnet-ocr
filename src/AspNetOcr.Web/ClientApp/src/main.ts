import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { ACTIVE_PRODUCT_CONFIG } from './app/branding/active-product';
import { provideClientHome } from './app/shell/client-home/client-home.token';
import { provideProductConfig } from './app/shell/product-config.token';

bootstrapApplication(AppComponent, {
  providers: [
    provideAnimations(),
    provideHttpClient(withFetch()),
    provideRouter(routes, withComponentInputBinding()),
    provideProductConfig(ACTIVE_PRODUCT_CONFIG),
    provideClientHome()
  ]
}).catch((error: unknown) => console.error(error));
