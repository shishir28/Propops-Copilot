import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';

export type PortalTheme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = 'propops.portal.theme';
  private readonly themeState = signal<PortalTheme>(this.readStoredTheme());

  readonly theme = computed(() => this.themeState());

  constructor() {
    this.applyTheme(this.themeState());
  }

  setTheme(theme: PortalTheme): void {
    this.themeState.set(theme);
    this.applyTheme(theme);
    localStorage.setItem(this.storageKey, theme);
  }

  private applyTheme(theme: PortalTheme): void {
    this.document.documentElement.setAttribute('data-theme', theme);
  }

  private readStoredTheme(): PortalTheme {
    const storedTheme = localStorage.getItem(this.storageKey);
    return storedTheme === 'dark' ? 'dark' : 'light';
  }
}
