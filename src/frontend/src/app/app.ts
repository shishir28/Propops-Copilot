import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { PortalTheme, ThemeService } from './core/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly themeService = inject(ThemeService);

  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly currentUser = this.authService.user;
  protected readonly activeTheme = this.themeService.theme;

  protected logout(): void {
    this.authService.logout();
  }

  protected setTheme(theme: PortalTheme): void {
    this.themeService.setTheme(theme);
  }
}
