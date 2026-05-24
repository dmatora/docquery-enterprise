import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AccessKeyService {
  private readonly storageKey = 'docquery.access-key';
  private readonly accessKeyState = signal(this.readStoredAccessKey());

  readonly accessKey = this.accessKeyState.asReadonly();
  readonly hasAccessKey = computed(() => this.accessKeyState().length > 0);

  setAccessKey(accessKey: string): void {
    const normalizedAccessKey = accessKey.trim();

    this.accessKeyState.set(normalizedAccessKey);
    window.localStorage.setItem(this.storageKey, normalizedAccessKey);
  }

  clearAccessKey(): void {
    this.accessKeyState.set('');
    window.localStorage.removeItem(this.storageKey);
  }

  private readStoredAccessKey(): string {
    return window.localStorage.getItem(this.storageKey)?.trim() ?? '';
  }
}
