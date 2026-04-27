import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  const storageKey = 'propops.portal.theme';

  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  afterEach(() => {
    const document = TestBed.inject(DOCUMENT);
    document.documentElement.removeAttribute('data-theme');
    localStorage.clear();
  });

  it('applies the stored theme when the service initializes', () => {
    localStorage.setItem(storageKey, 'dark');
    const service = TestBed.inject(ThemeService);
    const document = TestBed.inject(DOCUMENT);

    expect(service.theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('persists and applies theme changes', () => {
    const service = TestBed.inject(ThemeService);
    const document = TestBed.inject(DOCUMENT);

    service.setTheme('dark');

    expect(service.theme()).toBe('dark');
    expect(localStorage.getItem(storageKey)).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
